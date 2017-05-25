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
namespace Ao3Track {
    export namespace Utils {

        let escTagStrings = ["*s*", "*a*", "*d*", "*q*", "*h*"];
        let usescTagStrings = ["/", "&", ".", "?", "#"];
        let regexEscTag = new RegExp("([/&.?#])", "g");
        let regexUnescTag = new RegExp("(\\*[sadqh])\\*", "g");

        export function escapeTag(tag: string): string {
            return tag.replace(regexEscTag, (match) => {
                let i = usescTagStrings.indexOf(match);
                if (i !== -1) { return escTagStrings[i]; }
                return "";
            });
        }

        export function unescapeTag(tag: string): string {
            return tag.replace(regexUnescTag, (match) => {
                let i = escTagStrings.indexOf(match);
                if (i !== -1) { return usescTagStrings[i]; }
                return "";
            });
        }        
    }    
}
