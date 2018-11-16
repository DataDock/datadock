/// <binding BeforeBuild='clean, min, node_modules_copy' Clean='clean' />
/*
This file is the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkId=518007
*/

/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    merge = require('merge-stream'),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify-es").default,
    rename = require("gulp-rename");


var paths = {
    webroot: "./wwwroot/"
};

paths.js = paths.webroot + "js/**/*.js";
paths.minJs = paths.webroot + "js/**/*.min.js";
paths.css = paths.webroot + "css/**/*.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatJsDest = paths.webroot + "js/site.js";
paths.concatMinJsDest = paths.webroot + "js/site.min.js";
paths.concatCssDest = paths.webroot + "css/site.min.css";

gulp.task("clean:js", function (cb) {
    rimraf(paths.concatJsDest, cb);
    rimraf(paths.concatMinJsDest, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.concatCssDest, cb);
});

gulp.task("clean", ["clean:js", "clean:css"]);

gulp.task("min:js", function () {
    return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
        .pipe(concat(paths.concatJsDest))
        .pipe(gulp.dest("."))
        .pipe(uglify())
        .pipe(rename(paths.concatMinJsDest))
        .pipe(gulp.dest("."));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min", ["min:js", "min:css"]);


// Dependency Dirs
var nm = {
    "jquery": {
        "dist/*": ""
    },
    "@aspnet": {
        "signalr/dist/browser/*": ""
    }
};

gulp.task("nm_copy", function () {

    var streams = [];

    for (var prop in nm) {
        console.log("Prepping Scripts for: " + prop);
        for (var itemProp in nm[prop]) {
            var dest = prop.replace("@", "");
            streams.push(gulp.src("node_modules/" + prop + "/" + itemProp)
                .pipe(gulp.dest("wwwroot/vendor/" + dest + "/" + nm[prop][itemProp])));
        }
    }

    return merge(streams);

});

gulp.task('default', function () {
    // place code for your default task here
});