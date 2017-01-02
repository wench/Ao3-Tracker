namespace Ao3Track {
    namespace UnitConv {

        const value = "(\\d+\\.\\d+|\\d+)";
        const end = "(?=\\.|,|\\s|$)";
        const ws = "\\s*";
        const wsp = "\\s+";

        function toFloat(input: (string | number)): number {
            if (typeof (input) === "number") {
                return input;
            }
            return parseFloat(input);
        }

        abstract class UnitRegex {
            protected regex: RegExp;
            constructor(regex: RegExp) {
                this.regex = regex;
            }
            abstract process(values: (string | number)[]): string | null;
            toString(): string {
                return this.regex.source;
            };
            convert(src: string): string | null {
                let matches = src.match(this.regex);
                if (matches && matches.length > 1) {
                    return this.process(matches);
                }
                else {
                    return null;
                }
            }
        }

        class TemperatureF extends UnitRegex {
            constructor(regex: RegExp) {
                super(regex);
            }
            process(values: (string | number)[]): string {
                return (Math.floor((toFloat(values[1]) - 32) * 10 * 5 / 9) / 10).toString() + "\xB0 C";
            }
        }

        abstract class MultiUnitRegex extends UnitRegex {
            protected groups: string[];
            constructor(regex: RegExp, groups: string[]) {
                super(regex);
                this.groups = groups;
            }
        }
        class DistanceUS extends MultiUnitRegex {
            constructor(regex: RegExp, groups: string[]) {
                super(regex, groups);
            }
            process(values: (string | number)[]): string {
                let inches = 0;
                let onlyfeet = true;

                // Get total value in inches
                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "in":
                                inches += value;
                                onlyfeet = false;
                                break;

                            case "ft":
                                inches += value * 12;
                                break;

                            case "yd":
                                inches += value * 12 * 3;
                                onlyfeet = false;
                                break;

                            case "ch":
                                inches += value * 12 * 66;
                                onlyfeet = false;
                                break;

                            case "fur":
                                inches += value * 12 * 660;
                                onlyfeet = false;
                                break;

                            case "mi":
                                inches += value * 12 * 5280;
                                onlyfeet = false;
                                break;

                            case "lea":
                                inches += value * 12 * 15840;
                                onlyfeet = false;
                                break;

                            case "ftm":
                                inches += value * 12 * 6;
                                onlyfeet = false;
                                break;

                            case "cable":
                                inches += value * 18520 / 2.54; // Defined as exactly 185.2m
                                onlyfeet = false;
                                break;

                            case "nmi":
                                inches += value * 185200 / 2.54; // Defined as exactly 1852m
                                onlyfeet = false;
                                break;

                            case "link":
                                inches += value * 12 * 66 / 100;
                                onlyfeet = false;
                                break;

                            case "rod":
                                inches += value * 12 * 66 / 4;
                                onlyfeet = false;
                                break;
                        }
                    }
                }

                // Then convert to metric
                let cm = inches * 2.54;

                if (cm < 10) {
                    // below 10cm: show as mm
                    return Math.floor(cm * 10).toString() + " mm";
                }
                else if (cm < 200) {
                    // below 2m: show as cm
                    return Math.floor(cm).toString() + " cm";
                }
                else if (cm < 10000) {
                    // below 100m: show as m with 1 decimal place
                    return (Math.floor(cm / 10) / 10).toString() + " m";
                }
                else if (onlyfeet || cm < 100000) {
                    // below 1km: show as m
                    return Math.floor(cm / 100).toString() + " m";
                }
                else if (cm < 1000000) {
                    // below 10km: show as km with 1 decimal place
                    return (Math.floor(cm / 10000) / 10).toString() + " km";
                }
                else {
                    // Show as km
                    return Math.floor(cm / 100000).toString() + " km";
                }
            }
        }

        class VolumeUS extends MultiUnitRegex {
            constructor(regex: RegExp, groups: string[]) {
                super(regex, groups);
            }
            process(values: (string | number)[]): string {
                let ml = 0;

                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "oz":
                                ml += value * 29.5735295625;
                                break;

                            case "gi":
                                ml += value * 118.29411825;
                                break;

                            case "pt":
                                ml += value * 473.176473;
                                break;

                            case "qt":
                                ml += value * 946.352946;
                                break;

                            case "gal":
                                ml += value * 3785.411784;
                                break;
                        }
                    }
                }


                if (ml < 1) {
                    // below 1ml
                    return (Math.floor(ml * 100) / 100).toString() + " ml";
                }
                else if (ml < 10) {
                    // below 10ml
                    return (Math.floor(ml * 10) / 10).toString() + " ml";
                }
                else if (ml < 1000) {
                    // below 1l
                    return Math.floor(ml).toString() + " ml";
                }
                else if (ml < 100000) {
                    // below 100l
                    return (Math.floor(ml / 100) / 10).toString() + " l";
                }
                else {
                    return Math.floor(ml / 1000).toString() + " l";
                }
            }
        }

        class WeightUS extends MultiUnitRegex {
            constructor(regex: RegExp, groups: string[]) {
                super(regex, groups);
            }
            process(values: (string | number)[]): string {
                let g = 0;

                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "oz":
                                g += value * 28.349523125;
                                break;

                            case "lb":
                                g += value * 453.59237;
                                break;

                            case "ton":
                                g += value * 907184.74;
                                break;
                        }
                    }
                }


                if (g < 1) {
                    // below 1g
                    return (Math.floor(g * 100) / 100).toString() + " g";
                }
                else if (g < 10) {
                    // below 10g
                    return (Math.floor(g * 10) / 10).toString() + " g";
                }
                else if (g < 1000) {
                    // below 1kg
                    return Math.floor(g).toString() + " g";
                }
                else if (g < 100000) {
                    // below 100kg
                    return (Math.floor(g / 100) / 10).toString() + " kg";
                }
                else {
                    return Math.floor(g / 1000).toString() + " kg";
                }
            }
        }

        class WordyRegex extends UnitRegex {
            static number_words: string[] = [
                "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"
            ];
            static fraction_chars: string[] = [
                '\u215B',   // 1/8
                '\xBC',     // 1/4
                '\u215C',   // 3/8
                '\xBD',     // 1/2
                '\u215D',   // 5/8
                '\xBE',     // 3/4
                '\u215E',   // 7/8

                '\u2159',   // 1/6
                '\u2153',   // 1/3
                '',
                '\u2154',   // 2/3
                '\u215A',   // 5/6

                '\u2155',   // 1/5
                '\u2156',   // 2/5
                '\u2157',   // 3/5
                '\u2158',   // 4/5

                '\u2150',   // 1/7
                '\u2151',   // 1/9
                '\u2152',   // 1/10
            ];
            static fraction_words: string[] = [
                "quarter",
                "third",
                "half"
            ];
            static value: string =
            "(" + WordyRegex.number_words.join('|') +
            "|" + '[' + WordyRegex.fraction_chars.join('') + ']' +
            "|" + WordyRegex.fraction_words.join('|') +
            "|" + "(?:\\d+/\\d+)" +
            ")" +
            "(?:\\s+of)?" +
            "(?:\\s+a)?"
            ;

            conv: UnitRegex;
            constructor(exp: string, conv: UnitRegex) {
                super(new RegExp(WordyRegex.value + wsp + exp + end, "i"));
                this.conv = conv;
            }
            process(values: string[]): string | null {
                let i = WordyRegex.number_words.indexOf(values[1]);
                if (i !== -1) {
                    return this.conv.process([values[0], i + 1]);
                }
                i = WordyRegex.fraction_chars.indexOf(values[1]);
                if (i !== -1) {
                    let v: number;
                    // 0 - 6 are eighths
                    if (i <= 6) { v = (i + 1) / 8; }
                    // 7-11 are sixths
                    else if (i <= 11) { v = (i - 6) / 6; }
                    // 12-15 are sixths
                    else if (i <= 15) { v = (i - 11) / 6; }
                    // 1/7
                    else if (i === 16) { v = 1 / 7; }
                    // 1/9
                    else if (i === 17) { v = 1 / 9; }
                    // 1/10
                    else if (i === 18) { v = 1 / 10; }
                    else {
                        return null;
                    }
                    return this.conv.process([values[0], v]);
                }
                i = WordyRegex.fraction_words.indexOf(values[1]);
                if (i !== -1) {
                    let v: number;
                    if (i === 0) {
                        v = 1/4;
                    }
                    else if (i === 1) {
                        v = 1/3;
                    }
                    else if (i === 2) {
                        v = 1/2;
                    }
                    else {
                        return null;
                    }
                    return this.conv.process([values[0], v]);
                }
                var split = values[1].split('/');
                if (split && split.length === 2) {
                    return this.conv.process([values[0], toFloat(split[0]) / toFloat(split[1])]);
                }

                return null;
            }
        }

        let distancesUS: { name: string; exp: string; }[] = [
            { name: "mi", exp: "(?:mi|mile)s?" },
            { name: "fur", exp: "(?:fur|furlong)s?" },
            { name: "ch", exp: "(?:ch|chain)s?" },
            { name: "yd", exp: "(?:yd|yard)s?" },
            { name: "ft", exp: "(?:ft|fts|foot|feet|'|\u2032)" },
            { name: "in", exp: "(?:in|ins|inch|inches|\"|\u2033)" },
        ];

        let volumesUS: { name: string; exp: string; alt: string | null }[] = [
            { name: "qt", exp: "(?:qt|quart)s?", alt: null },
            { name: "pt", exp: "(?:pt|pint)s?", alt: null },
            { name: "gi", exp: "(?:gi|gill)s?", alt: null },
            { name: "oz", exp: "(?:fl[ -]?oz|fluid[ -]ounce|fluid[ -]ounces)", alt: "(?:fl[ -]?oz|oz|fluid[ -]ounce|ounce|fluid[ -]ounces|ounces)", },
        ];

        let weightsUS: { name: string; exp: string; }[] = [
            { name: "ton", exp: "(?:ton|short[ -?]ton|us[ -?]ton)s?" },
            { name: "lb", exp: "(?:lb|pound)s?" },
            { name: "oz", exp: "(?:oz|ounce|ounces)" },
        ];

        let convs: UnitRegex[] = [
            new TemperatureF(new RegExp(value + "(?:" + ws + "(?:degree|degrees|F|\xB0))+" + end, "i"))
        ];

        for (let s = 0; s < distancesUS.length; s++) {
            let exp = value + ws + distancesUS[s].exp;
            let units = [distancesUS[s].name];
            for (let i = s + 1; i < distancesUS.length; i++) {
                exp = exp + "(?:" + ws + value + ws + distancesUS[i].exp + ")?";
                units.push(distancesUS[i].name);
            }
            let conv = new DistanceUS(new RegExp(exp + end, "i"), units);
            convs.push(conv);
            convs.push(new WordyRegex(distancesUS[s].exp,conv));
        }
        for (let s = 0; s < volumesUS.length; s++) {
            let exp = value + ws + volumesUS[s].exp;
            let units = [volumesUS[s].name];
            for (let i = s + 1; i < volumesUS.length; i++) {
                exp = exp + "(?:" + ws + value + ws + (volumesUS[i].alt || volumesUS[i].exp) + ")?";
                units.push(volumesUS[i].name);
            }
            let conv = new VolumeUS(new RegExp(exp + end, "i"), units);
            convs.push(conv);
            convs.push(new WordyRegex(distancesUS[s].exp,conv));
        }
        for (let s = 0; s < weightsUS.length; s++) {
            let exp = value + ws + weightsUS[s].exp;
            let units = [weightsUS[s].name];
            for (let i = s + 1; i < weightsUS.length; i++) {
                exp = exp + "(?:" + ws + value + ws + weightsUS[i].exp + ")?";
                units.push(weightsUS[i].name);
            }
            let conv = new WeightUS(new RegExp(exp + end, "i"), units);
            convs.push(conv);
            convs.push(new WordyRegex(distancesUS[s].exp,conv));
        }

        let all_regex = new RegExp('((?:' + convs.join(')|(?:').replace(/\((?!\?)/g, "(?:") + '))', 'gi');

        function ignoreElement(elem: Element) {
            if (elem.tagName === "SPAN" && elem.hasAttribute("class")) {
                let classes = (elem.getAttribute("class") || "").split(" ");
                if (classes.indexOf("mw_ao3track_unitconv") !== -1) {
                    return true;
                }
            }
            if (elem.tagName === "LINK" || elem.tagName === "SCRIPT" || elem.tagName === "HEAD" || elem.getAttribute("contenteditable") === "true") {
                return true;
            }
            return false;
        }

        let text_nodes: Node[] = [];

        function gatherUnitTextNodes(node: Node) {
            if (node.nodeType === 3) {
                if (node.nodeValue !== null && node.nodeValue.search(all_regex) !== -1) {
                    text_nodes.push(node);
                }
            } else {
                if (node.nodeType === 1 && ignoreElement(node as Element)) {
                    return;
                }

                for (let i = 0, len = node.childNodes.length; i < len; ++i) {
                    gatherUnitTextNodes(node.childNodes[i]);
                }
            }
        }

        gatherUnitTextNodes(document.body);

        function revertSpan(ev: MouseEvent) {
            let target = ev.target as HTMLSpanElement;
            target.innerText = target.getAttribute("mw_ao3track_unitconv") || "";
            target.onmousedown = convertSpan;
        };

        function convertSpan(ev: MouseEvent) {
            let target = ev.target as HTMLSpanElement;
            for (let c of convs) {
                let replacement = c.convert(target.innerText);
                if (replacement !== null) {
                    target.onmousedown = revertSpan;
                    target.innerText = replacement;
                    break;
                }
            }
        };

        for (let i = 0; i < text_nodes.length; ++i) {
            let split = (text_nodes[i].nodeValue || "").split(all_regex);
            let fragment = document.createDocumentFragment();
            for (let s = 0; s < split.length; s++) {
                if (split[s] !== '') {
                    let textnode = document.createTextNode(split[s]);
                    fragment.appendChild(textnode);
                }
                s++;
                if (s < split.length) {
                    let unitnode = document.createElement("span") as HTMLSpanElement;
                    unitnode.setAttribute("class", "mw_ao3track_unitconv");
                    unitnode.setAttribute("mw_ao3track_unitconv", split[s]);
                    unitnode.innerText = split[s];
                    unitnode.onmousedown = convertSpan;
                    fragment.appendChild(unitnode);
                }
            }
            let parent = text_nodes[i].parentNode;
            if (parent !== null) {
                parent.replaceChild(fragment, text_nodes[i]);
            }
        }
    }
}