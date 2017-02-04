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
