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

    // Page title handling
    let pageTitle: IPageTitle = { title: jQuery("h2.heading").first().text().trim() };
    if (pageTitle.title === null || pageTitle.title === "" || pageTitle.title === undefined) {
        pageTitle.title = document.title;
        pageTitle.title = pageTitle.title.endsWith(" | Archive of Our Own") ? pageTitle.title.substr(0, pageTitle.title.length - 21) : pageTitle.title;
        pageTitle.title = pageTitle.title.endsWith(" [Archive of Our Own]") ? pageTitle.title.substr(0, pageTitle.title.length - 21) : pageTitle.title;
    }
    else {
        let $c = jQuery("#chapters h3.title");
        if ($c.length > 0) {
            let $num = $c.children("a").first();
            if ($num.length > 0) {
                pageTitle.chapter = $num.text().trim();

                let name = "";

                for (let node = $num[0].nextSibling; node !== null; node = node.nextSibling) {
                    if (node.nodeType === Node.TEXT_NODE) {
                        name += (node.textContent || "");
                    }
                }

                name = name.trim();
                if (name.startsWith(":")) { name = name.substr(1); }
                name = name.trim();
                if (name.length > 0 && name !== pageTitle.chapter) {
                    pageTitle.chaptername = name;
                }
            }
        }

        let $authors = jQuery("h3.heading a[rel=author]");
        if ($authors.length > 0) {
            let authors: string[] = [];
            $authors.each((index, elem) => {
                if (elem.textContent) { authors.push(elem.textContent.trim()); }
            });
            if (authors.length > 0) { pageTitle.authors = authors; }
        }

        let $fandoms = jQuery(".meta dd.fandom.tags a.tag");
        if ($fandoms.length > 0) {
            let fandoms: string[] = [];
            $fandoms.each((index, elem) => {
                if (elem.textContent) { fandoms.push(elem.textContent.trim()); }
            });
            if (fandoms.length > 0) { pageTitle.fandoms = fandoms; }
        }

        let $relationships = jQuery(".meta dd.relationship.tags a.tag").first();
        if ($relationships.length > 0) {
            pageTitle.primarytag = $relationships.text().trim() || null;
        }
    }

    Ao3Track.Helper.pageTitle = pageTitle;
};
