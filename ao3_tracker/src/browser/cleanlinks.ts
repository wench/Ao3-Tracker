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

interface Window {
    fetch(req: any, opts: any): any;
};
namespace Ao3Track {
    namespace CleanLinks {
        // Go through and unshorten/clean CleanLinks
        let elems = document.getElementsByTagName("a");

        for (let index = 0; index < elems.length; index++) {
            let link = elems[index];
            let original = new URL(link.href, document.URL);
            let url = original;
            if (url.hostname === "t.umblr.com" && url.pathname === "/redirect") {
                let search = url.search;
                let z: string | undefined;
                if (search[0] === '?') { search = search.substr(1); }
                for (let comp of search.split(/[;&]/)) {
                    let kvp = comp.split("=", 2);
                    if (kvp[0] === "z") {
                        z = kvp[1];
                        break;
                    }
                }
                if (!z) { continue; }
                url = new URL(decodeURIComponent(z));
            }

            if (url.hostname === "archiveofourown.org") {
                if (original.href !== url.href) {
                    link.href = url.href;
                }
                continue;
            }
            /*else if (url.hostname === "ift.tt" || url.hostname === "bit.ly" || url.hostname === "t.co") {
                window.fetch(url.href, { method: "HEAD", redirect: "manual", mode: "no-cors" }).then((response: any) => {
                    if (url.href !== response.url) {
                        link.href = response.url;
                    }
                }).catch(function(reason:any) {
                });
            }*/
        };
    };

}
