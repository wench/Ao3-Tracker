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
    export namespace CSS {
        export function Inject(styles: string) {
            /*
            let blob = new Blob([styles], { type: 'text/css', endings: "transparent" });
            let link = document.createElement('link');
            link.type = 'text/css';
            link.rel = 'stylesheet';
            link.href = URL.createObjectURL(blob);
            document.head.appendChild(link);
            */
            let elem = document.createElement('style');
            elem.type = 'text/css';
            elem.textContent = styles;
            document.head.appendChild(elem);
        };
        
        let init = () => {
            if (document.head) {
                Inject('.blurb.work[id] { visibility: hidden; }');
            }
            else {
                setImmediate(init);
            }
        };
        init();
    }
}