/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/


interface ObjectConstructor
{
    assign(target : any, varArgs : any) : any;
}

if (typeof Object.assign !== 'function') {
  Object.assign = function(target : any, varArgs : any) { // .length of function is 2
    'use strict';
    if (target === null) { // TypeError if undefined or null
      throw new TypeError('Cannot convert undefined or null to object');
    }

    let to = Object(target);

    for (let index = 1; index < arguments.length; index++) {
      let nextSource = arguments[index];

      if (nextSource !== null) { // Skip over if undefined or null
        for (let nextKey in nextSource) {
          // Avoid bugs when hasOwnProperty is shadowed
          if (Object.prototype.hasOwnProperty.call(nextSource, nextKey)) {
            to[nextKey] = nextSource[nextKey];
          }
        }
      }
    }
    return to;
  };
}

interface String
{
    endsWith(searchString: string, position?: number) : boolean;
    startsWith(searchString: string, position?: number) : boolean;
}

if (!String.prototype.endsWith) {
  String.prototype.endsWith = function(searchString: string, position?: number) {
      let subjectString : string = this.toString();
      if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
        position = subjectString.length;
      }
      position -= searchString.length;
      let lastIndex = subjectString.lastIndexOf(searchString, position);
      return lastIndex !== -1 && lastIndex === position;
  };
}

if (!String.prototype.startsWith) {
    String.prototype.startsWith = function(searchString: string, position?: number){
      position = position || 0;
      return this.substr(position, searchString.length) === searchString;
  };
}

interface StringConstructor {
    fromCodePoint(...codePoints: number[]): string;
}

/*! http://mths.be/fromcodepoint v0.1.0 by @mathias */
if (!String.fromCodePoint) {
  (function() {
    let defineProperty = Object.defineProperty;
    let stringFromCharCode = String.fromCharCode;
    let floor = Math.floor;
    let fromCodePoint = function() {
      let MAX_SIZE = 0x4000;
      let codeUnits = [];
      let highSurrogate;
      let lowSurrogate;
      let index = -1;
      let length = arguments.length;
      if (!length) {
        return '';
      }
      let result = '';
      while (++index < length) {
        let codePoint = Number(arguments[index]);
        if (
          !isFinite(codePoint) ||       // `NaN`, `+Infinity`, or `-Infinity`
          codePoint < 0 ||              // not a valid Unicode code point
          codePoint > 0x10FFFF ||       // not a valid Unicode code point
          floor(codePoint) !== codePoint // not an integer
        ) {
          throw RangeError('Invalid code point: ' + codePoint);
        }
        if (codePoint <= 0xFFFF) { // BMP code point
          codeUnits.push(codePoint);
        } else { // Astral code point; split in surrogate halves
          // http://mathiasbynens.be/notes/javascript-encoding#surrogate-formulae
          codePoint -= 0x10000;
          highSurrogate = (codePoint >> 10) + 0xD800;
          lowSurrogate = (codePoint % 0x400) + 0xDC00;
          codeUnits.push(highSurrogate, lowSurrogate);
        }
        if (index + 1 === length || codeUnits.length > MAX_SIZE) {
          result += stringFromCharCode.apply(null, codeUnits);
          codeUnits.length = 0;
        }
      }
      return result;
    };
    if (defineProperty) {
      defineProperty(String, 'fromCodePoint', {
        'value': fromCodePoint,
        'configurable': true,
        'writable': true
      });
    } else {
      String.fromCodePoint = fromCodePoint;
    }
  }());
}
