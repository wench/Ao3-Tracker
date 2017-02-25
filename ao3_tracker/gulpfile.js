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
    removeComments: true,
    preserveConstEnums: true,
    strictNullChecks: true
};

var tsOptions_ES5 = Object.assign({ 
    target: "ES5" 
},tsOptions);

var tsOptions_ES6 = Object.assign({ 
    target: "ES6" 
},tsOptions);

var browser_scripts = [
    'src/*.ts',
    'src/browser/*.ts'
];
var uwp_scripts = [
    'src/*.ts',
    'src/reader/uwp/*.ts',
    'src/reader/*.ts'
];
var droid_scripts = [
    'src/*.ts',
    'src/reader/webkit/*.ts',
    'src/reader/webkit/droid/*.ts',
    'src/reader/*.ts'
];

function scripts() {
    return gulp.src(browser_scripts)
        .pipe(sourcemaps.init())
        .pipe(ts(tsOptions_ES6))
        .pipe(sourcemaps.write('src-maps'))
        .pipe(gulp.dest('build/browser/chrome'))
        .pipe(gulp.dest('build/browser/edge'));
}
gulp.task('scripts', scripts);

var reader = {
    "reader.scripts.uwp": () => {
        return gulp.src(uwp_scripts)
            .pipe(sourcemaps.init())
            .pipe(ts(tsOptions_ES6))
            .pipe(sourcemaps.write('src-maps', {sourceMappingURL: (file) => { return url.parse("file:///" + file.cwd + '/build/reader/uwp/src-maps/' + file.basename + ".map").href; }}))
            .pipe(gulp.dest('build/reader/uwp'));
    },
    "reader.scripts.droid": () => {
        return gulp.src(droid_scripts)
        .pipe(sourcemaps.init())
        .pipe(ts(tsOptions_ES5))
        .pipe(sourcemaps.write('src-maps', {sourceMappingURL: (file) => { return url.parse("file:///" + file.cwd + '/build/reader/droid/src-maps/' + file.basename + ".map").href; }}))
        .pipe(gulp.dest('build/reader/droid'));
    },

    "reader.styles.uwp": () => {
        return gulp.src('src/*.less')
            .pipe(sourcemaps.init())
            .pipe(less())
            .pipe(sourcemaps.write('src-maps'))
            .pipe(gulp.dest('build/reader/uwp'));
    },
    "reader.styles.droid": () => {
        return gulp.src('src/*.less')
            .pipe(sourcemaps.init())
            .pipe(less())
            .pipe(sourcemaps.write('src-maps'))
            .pipe(gulp.dest('build/reader/droid'));
    },
};
exports["reader.scripts"] = reader.scripts = gulp.series(reader["reader.scripts.uwp"],reader["reader.scripts.droid"]);
exports["reader.scripts.uwp"] = reader.scripts.uwp = reader["reader.scripts.uwp"];
exports["reader.scripts.droid"] = reader.scripts.droid = reader["reader.scripts.droid"]

exports["reader.styles"] = reader.styles = gulp.series(reader["reader.styles.uwp"],reader["reader.styles.droid"]);
exports["reader.styles.uwp"] = reader.styles.uwp = reader["reader.styles.uwp"];
exports["reader.styles.droid"] = reader.styles.droid = reader["reader.styles.droid"];

exports["reader.uwp"] = reader.uwp = gulp.series(reader.scripts.uwp, reader.styles.uwp);
exports["reader.uwp.scripts"] = reader.uwp.scripts = reader.scripts.uwp;
exports["reader.uwp.styles"] = reader.uwp.styles = reader.styles.uwp;

exports["reader.droid"] = reader.droid = gulp.series(reader.scripts.droid, reader.styles.droid);
exports["reader.droid.scripts"] = reader.droid.scripts = reader.scripts.droid;
exports["reader.droid.styles"] = reader.droid.styles = reader.styles.droid;

exports["reader"] = reader = Object.assign(gulp.series(reader.scripts,reader.styles),reader);

function styles() {
    return gulp.src('src/*.less')
        .pipe(sourcemaps.init())
        .pipe(less())
        .pipe(sourcemaps.write('src-maps'))
        .pipe(gulp.dest('build/browser/edge'))
        .pipe(gulp.dest('build/browser/chrome'));
}
gulp.task('styles', styles);

function images() {
    return gulp.src('src/browser/*.png')
        .pipe(cache(imagemin({ optimizationLevel: 3, progressive: true, interlaced: true })))
        .pipe(gulp.dest('build/browser/edge'))
        .pipe(gulp.dest('build/browser/chrome'));
}
gulp.task('images', images);

function pages() {
    var edge = gulp.src('src/browser/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' } }))
        .pipe(gulp.dest('build/browser/edge'));

    var chrome = gulp.src('src/browser/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' } }))
        .pipe(gulp.dest('build/browser/chrome'));

    return merge(edge, chrome);
}
gulp.task('pages', pages);

function extras() {
    var edge = gulp.src('src/browser/edge/**/*')
        .pipe(gulp.dest('build/browser/edge'));

    var chrome = gulp.src('src/browser/chrome/**/*')
        .pipe(gulp.dest('build/browser/chrome'));

    return merge(edge, chrome);
}
gulp.task('extras', extras);

function libs() {
    return gulp.src('src/lib/**/*')
        .pipe(gulp.dest('build/browser/edge/lib'))
        .pipe(gulp.dest('build/browser/chrome/lib'));
}
gulp.task('libs', libs);

function json() {
    var edge = gulp.src('src/browser/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' }, extension: 'js' }))
        .pipe(gulp.dest('build/browser/edge'));

    var chrome = gulp.src('src/browser/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' }, extension: 'js' }))
        .pipe(gulp.dest('build/browser/chrome'));

    return merge(edge, chrome);
}
gulp.task('json', json);

var build = gulp.series(scripts, styles, images, pages, json, extras, libs, reader);
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


