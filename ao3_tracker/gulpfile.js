var gulp = require('gulp');
var ts = require('gulp-typescript');
var sourcemaps = require('gulp-sourcemaps');
var cache = require('gulp-cache');
var imagemin = require('gulp-imagemin');
var less = require('gulp-less');
var preprocess = require('gulp-preprocess');
var merge = require('merge-stream');

var tsOptions = {
    target: "es5",
    module: "none",
    noImplicitAny: true,
    removeComments: false,
    preserveConstEnums: true,
    strictNullChecks: true,
};

gulp.task('scripts', function() {
    return gulp.src('src/*.ts')
        .pipe(sourcemaps.init())
        .pipe(ts(tsOptions))
        .pipe(sourcemaps.write('maps'))
        .pipe(gulp.dest('build/chrome'))
        .pipe(gulp.dest('build/edge'));
});

var uwp_scripts = [
    'src/global.d.ts',
    'src/ao3_tracker.ts',
    'src/extras/reader/uwp/ao3_t_reader.ts'
];
var webkit_scripts = [
    'src/global.d.ts',
    'src/ao3_tracker.ts',
    'src/extras/reader/webkit/ao3_t_callbacks.ts',
    'src/extras/reader/webkit/ao3_t_reader.ts'
];

gulp.task('reader:uwp:scripts', function() {
    return gulp.src(uwp_scripts)
        .pipe(ts(tsOptions))
        .pipe(gulp.dest('build/reader/uwp'));
});
gulp.task('reader:webkit:scripts', function() {
    return gulp.src(webkit_scripts)
        .pipe(ts(tsOptions))
        .pipe(gulp.dest('build/reader/webkit'));
});

gulp.task('reader:uwp:styles', function() {
    return gulp.src('src/*.less')
        .pipe(less())
        .pipe(gulp.dest('build/reader/uwp'));
});
gulp.task('reader:webkit:styles', function() {
    return gulp.src('src/*.less')
        .pipe(less())
        .pipe(gulp.dest('build/reader/webkit'));
});

gulp.task('reader:uwp', ['reader:uwp:scripts', 'reader:uwp:styles']);
gulp.task('reader:webkit', ['reader:webkit:scripts', 'reader:webkit:styles']);
gulp.task('reader', ['reader:uwp','reader:webkit']);

gulp.task('styles', function() {
    return gulp.src('src/*.less')
        .pipe(sourcemaps.init())
        .pipe(less())
        .pipe(sourcemaps.write('maps'))
        .pipe(gulp.dest('build/edge'))
        .pipe(gulp.dest('build/chrome'));
});

gulp.task('images', function() {
    return gulp.src('src/*.png')
        .pipe(cache(imagemin({ optimizationLevel: 3, progressive: true, interlaced: true })))
        .pipe(gulp.dest('build/edge'))
        .pipe(gulp.dest('build/chrome'));
});

gulp.task('pages', function() {
    var edge = gulp.src('src/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' } }))
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/*.html')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' } }))
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
});

gulp.task('extras', function() {
    var edge = gulp.src('src/extras/edge/**/*')
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/extras/chrome/**/*')
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
});

gulp.task('libs', function() {
    return gulp.src('src/lib/**/*')
        .pipe(gulp.dest('build/edge/lib'))
        .pipe(gulp.dest('build/chrome/lib'));
});

gulp.task('json', function() {
    var edge = gulp.src('src/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Edge' }, extension: 'js' }))
        .pipe(gulp.dest('build/edge'));

    var chrome = gulp.src('src/*.json')
        .pipe(preprocess({ context:  {  BROWSER: 'Chrome' }, extension: 'js' }))
        .pipe(gulp.dest('build/chrome'));

    return merge(edge, chrome);
});

gulp.task('watch', ['default'], function() {
    gulp.watch('src/**/*.ts', ['scripts']);
    gulp.watch('src/**/*.less', ['styles']);
    gulp.watch('src/**/*.png', ['images']);
    gulp.watch('src/**/*.html', ['pages']);
    gulp.watch('src/**/*.json', ['json']);
    gulp.watch('src/extras/**/*', ['extras']);
    gulp.watch('src/lib/**/*', ['libs']);
});


gulp.task('default', ['scripts', 'styles', 'images', 'pages', 'json', 'extras', 'libs', 'reader']);