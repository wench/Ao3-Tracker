var gulp = require('gulp');
var ts = require('gulp-typescript');
var sourcemaps = require('gulp-sourcemaps');
var cache = require('gulp-cache');
var imagemin = require('gulp-imagemin');
var less = require('gulp-less');
var preprocess = require('gulp-preprocess');
var merge = require('merge-stream');
const url = require('url');

var tsOptions = {
    module: "none",
    noImplicitAny: true,
    removeComments: false,
    preserveConstEnums: true,
    strictNullChecks: true
};

var tsOptions_ES5 = Object.assign({ 
    target: "ES5" 
},tsOptions);

var tsOptions_ES6 = Object.assign({ 
    target: "ES6" 
},tsOptions);

function scripts() {
    return gulp.src('src/*.ts')
        .pipe(sourcemaps.init())
        .pipe(ts(tsOptions_ES6))
        .pipe(sourcemaps.write('src-maps'))
        .pipe(gulp.dest('build/chrome'))
        .pipe(gulp.dest('build/edge'));
}
gulp.task('scripts', scripts);

var uwp_scripts = [
    'src/global.d.ts',
    'src/ao3_tracker.ts',
    'src/ao3_t_unitconv.ts',
    'src/extras/reader/uwp/ao3_t_reader.ts'
];
var webkit_scripts = [
    'src/global.d.ts',
    'src/ao3_tracker.ts',
    'src/ao3_t_unitconv.ts',
    'src/extras/reader/webkit/ao3_t_callbacks.ts',
    'src/extras/reader/webkit/ao3_t_reader.ts'
];

var reader = {
    scripts: {
        uwp: function reader_scripts_uwp() {
            return gulp.src(uwp_scripts)
                .pipe(sourcemaps.init())
                .pipe(ts(tsOptions_ES6))
                .pipe(sourcemaps.write('src-maps', {sourceMappingURL: (file) => { return url.parse("file:///" + file.cwd + '/build/reader/uwp/src-maps/' + file.basename + ".map").href; }}))
                .pipe(gulp.dest('build/reader/uwp'));
        },
        webkit: function reader_scripts_webkit() {
           return gulp.src(webkit_scripts)
            .pipe(sourcemaps.init())
            .pipe(ts(tsOptions_ES6))
            .pipe(sourcemaps.write('src-maps', {sourceMappingURL: (file) => { return url.parse("file:///" + file.cwd + '/build/reader/webkit/src-maps/' + file.basename + ".map").href; }}))
            .pipe(gulp.dest('build/reader/webkit'));
        },
    },

    styles: {
        uwp: function reader_styles_uwp() {
            return gulp.src('src/*.less')
                .pipe(sourcemaps.init())
                .pipe(less())
                .pipe(sourcemaps.write('src-maps'))
                .pipe(gulp.dest('build/reader/uwp'));
        },
        webkit: function reader_styles_webkit() {
            return gulp.src('src/*.less')
                .pipe(sourcemaps.init())
                .pipe(less())
                .pipe(sourcemaps.write('src-maps'))
                .pipe(gulp.dest('build/reader/webkit'));
        },
    },
};
exports.reader_scripts = reader.scripts.all = gulp.series(reader.scripts.uwp,reader.scripts.webkit);
exports.reader_styles = reader.styles.all = gulp.series(reader.styles.uwp,reader.styles.webkit);
exports.reader_uwp = reader.uwp = gulp.series(reader.scripts.uwp, reader.styles.uwp);
exports.reader_webkit = reader.webkit = gulp.series(reader.scripts.webkit, reader.styles.webkit);
exports.reader = reader.all = gulp.series(reader.scripts.all,reader.styles.all);

exports.reader_scripts_uwp = reader.scripts.uwp;
exports.reader_styles_uwp = reader.styles.uwp;
exports.reader_scripts_webkit = reader.scripts.webkit;
exports.reader_styles_webkit = reader.styles.webkit;

function styles() {
    return gulp.src('src/*.less')
        .pipe(sourcemaps.init())
        .pipe(less())
        .pipe(sourcemaps.write('src-maps'))
        .pipe(gulp.dest('build/edge'))
        .pipe(gulp.dest('build/chrome'));
}
gulp.task('styles', styles);

function images() {
    return gulp.src('src/*.png')
        .pipe(cache(imagemin({ optimizationLevel: 3, progressive: true, interlaced: true })))
        .pipe(gulp.dest('build/edge'))
        .pipe(gulp.dest('build/chrome'));
}
gulp.task('images', images);

function pages() {
    var edge = gulp.src('src/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' } }))
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' } }))
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
}
gulp.task('pages', pages);

function extras() {
    var edge = gulp.src('src/extras/edge/**/*')
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/extras/chrome/**/*')
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
}
gulp.task('extras', extras);

function libs() {
    return gulp.src('src/lib/**/*')
        .pipe(gulp.dest('build/edge/lib'))
        .pipe(gulp.dest('build/chrome/lib'));
}
gulp.task('libs', libs);

function json() {
    var edge = gulp.src('src/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' }, extension: 'js' }))
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' }, extension: 'js' }))
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
}
gulp.task('json', json);

var build = gulp.series(scripts, styles, images, pages, json, extras, libs, reader.all);
gulp.task('default', build);

gulp.task('watch', gulp.series(build, function() {
    gulp.watch('src/**/*.ts', scripts);
    gulp.watch('src/**/*.less', styles);
    gulp.watch('src/**/*.png', images);
    gulp.watch('src/**/*.html', pages);
    gulp.watch('src/**/*.json', json);
    gulp.watch('src/extras/**/*', extras);
    gulp.watch('src/lib/**/*', libs);
}));


