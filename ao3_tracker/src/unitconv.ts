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
    export enum UnitConvSetting
    {
        None = 0,
        MetricToUS = 1,
        USToMetric = 2
    }

    export interface IUnitConvSettings
    {
        temp : UnitConvSetting;
        dist : UnitConvSetting;
        volume : UnitConvSetting;
        weight : UnitConvSetting;
    }
    
    namespace UnitConv {

        const valueExp = "(\\d+\\.\\d+||\\d+)";
        const valueNegExp = "(-|minus|negative)?\s*(\\d*\\.\\d+|\\d+)";
        const ws = "\\s*?";
        const wsp = "\\s+";

        function toFloat(input: (string | number)): number {
            if (typeof (input) === "number") {
                return input;
            }
            return parseFloat(input);
        }

        interface UnitType {
            name: string;
            exp: string;
            alt?: string;
        }

        abstract class UnitRegex {

            protected regex: RegExp | null;
            constructor(regex: RegExp | null) {
                this.regex = regex;
            }
            abstract process(values: (string | number)[]): string | null;
            toString(): string {
                if (this.regex === null) { return ""; }
                return this.regex.source;
            };
            convert(src: string): string | null {
                if (this.regex === null) { return null; }
                let matches = src.match(this.regex);
                if (matches && matches.length > 1) {
                    return this.process(matches);
                }
                else {
                    return null;
                }
            }
        }

        class TemperatureFtoC extends UnitRegex {
            constructor(regex: RegExp) {
                super(regex);
            }
            process(values: (string | number)[]): string {
                let mul = (values[1] || values[3]) ? -1 : 1;
                let value = Math.floor((toFloat(values[2]) * mul - 32) * 10 * 5 / 9) / 10;
                return value.toString() + "\xB0 C";
            }
        }

        class TemperatureCtoF extends UnitRegex {
            constructor(regex: RegExp) {
                super(regex);
            }
            process(values: (string | number)[]): string {
                let mul = (values[1] || values[3]) ? -1 : 1;
                let value = Math.floor((toFloat(values[2]) * mul * 9 / 5 + 32));
                return value.toString() + "\xB0 F";
            }
        }

        abstract class MultiUnitRegex extends UnitRegex {
            protected groups: string[];
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex);
                this.groups = groups;
            }
        }

        class DistanceUStoM extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "mi", exp: "(?:mi|mile)s?" },
                { name: "fur", exp: "(?:fur|furlong)s?" },
                { name: "ch", exp: "(?:ch|chain)s?" },
                { name: "yd", exp: "(?:yd|yard)s?" },
                { name: "ft", exp: "(?:ft|fts|foot|feet|'|\u2032)" },
                { name: "in", exp: "(?:in|ins|inch|inches|\"|\u2033)" },
            ];

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

        class DistanceMtoUS extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "km", exp: "(?:km|(?:kilometre|kilometer)s?)" },
                { name: "m", exp: "(?:m|(?:metre|meter)s?)" },
                { name: "cm", exp: "(?:cm|(?:centimetre|centimeter)s?)" },
                { name: "mm", exp: "(?:mm|(?:millimetre|millimeter)s?)" },
            ];

            process(values: (string | number)[]): string {
                let mm = 0;
                let only_metres = true;

                // Get total value in inches
                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "km":
                                mm += value * 1000 * 1000;
                                only_metres = false;
                                break;

                            case "m":
                                mm += value * 1000;
                                break;

                            case "cm":
                                mm += value * 10;
                                only_metres = false;
                                break;

                            case "mm":
                                mm += value;
                                only_metres = false;
                                break;
                        }
                    }
                }

                // Then convert to US
                let inches = mm / 25.4;

                if (inches < 12) {
                    return (Math.floor(inches * 20) / 20).toString() + "\"";
                }

                let feet = Math.floor(inches / 12);
                inches = Math.floor(inches - feet * 12);

                if (feet < 12) {
                    return feet.toString() + "\'" + (inches ? " " + inches.toString() + "\"" : "");
                }
                else if (feet < 1200 || only_metres) {
                    return feet.toString() + "\'";
                }
                else {
                    let miles = feet / 5280;
                    return (Math.floor(miles * 20) / 20).toString() + " miles";
                }
            }
        }

        class VolumeUStoM extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "gal", exp: "(?:gal|gallon)s?" },
                { name: "qt", exp: "(?:qt|quart)s?" },
                { name: "pt", exp: "(?:pt|pint)s?" },
                { name: "gi", exp: "(?:gi|gill)s?" },
                { name: "oz", exp: "(?:fl[ -]?oz|fluid[ -]ounce|fluid[ -]ounces)", alt: "(?:fl[ -]?oz|oz|fluid[ -]ounce|ounce|fluid[ -]ounces|ounces)", },
            ];

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


        class VolumeMtoUS extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "kl", exp: "(?:kl|m3|m^3|m\xB3|(?:kilolitre|kiloliter)s?)" },
                { name: "l", exp: "(?:l|(?:litre|liter)s?)" },
                { name: "ml", exp: "(?:ml|cc|cm3|cm^3|cm\xB3|(?:millilitre|milliliter)s?)" },
            ];

            process(values: (string | number)[]): string {
                let ml = 0;

                // Get total value in inches
                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "kl":
                                ml += value * 1000 * 1000;
                                break;

                            case "l":
                                ml += value * 1000;
                                break;

                            case "ml":
                                ml += value;
                                break;
                        }
                    }
                }

                if (ml < 29.5735295625) { // Less than 1 floz
                    return (Math.floor(ml * 100 / 29.5735295625) / 100).toString() + " floz";
                }
                else if (ml < 295.735295625) { // Less than 10 floz
                    return (Math.floor(ml * 10 / 29.5735295625) / 10).toString() + " floz";
                }
                else if (ml < 3785.411784) { // Less than 1 gal
                    return Math.floor(ml / 29.5735295625).toString() + " floz";
                }
                else if (ml < 37854.11784) { // Less than 10 gal
                    return (Math.floor(ml * 10 / 37854.11784) / 10).toString() + " gal";
                }
                else {
                    return Math.floor(ml / 37854.11784).toString() + " gal";
                }
            }
        }

        class WeightUStoM extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "ton", exp: "(?:ton|short[ -?]ton|us[ -?]ton)s?" },
                { name: "st", exp: "(?:st|stone)s?" },
                { name: "lb", exp: "(?:lb|pound)s?" },
                { name: "oz", exp: "(?:oz|ounce|ounces)" },
            ];

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

                            case "st":
                                g += value * 453.59237 * 14;
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


        class WeightMtoUS extends MultiUnitRegex {
            constructor(regex: RegExp | null, groups: string[]) {
                super(regex, groups);
            }

            static readonly units: UnitType[] = [
                { name: "t", exp: "(?:t|(?:ton|tonne|metric ton)s?)" },
                { name: "kg", exp: "(?:kg|(?:kilogram|kilogramme)s?)" },
                { name: "g", exp: "(?:g|(?:gram|gramme)s?)" },
            ];

            process(values: (string | number)[]): string {
                let g = 0;

                // Get total value in inches
                for (let i = 1; i <= this.groups.length && i < values.length; i++) {
                    if (values[i]) {
                        let value = toFloat(values[i]);
                        switch (this.groups[i - 1]) {
                            case "t":
                                g += value * 1000 * 1000;
                                break;

                            case "kg":
                                g += value * 1000;
                                break;

                            case "g":
                                g += value;
                                break;
                        }
                    }
                }

                if (g < 28.349523125) { // Less than 1 oz
                    return (Math.floor(g * 100 / 28.349523125) / 100).toString() + " oz";
                }
                else if (g < 453.59237) { // Less than 1 lb
                    return (Math.floor(g * 10 / 28.349523125) / 10).toString() + " oz";
                }
                else if (g < 63502.9318) { // Less than 10 stone
                    let oz = g / 28.349523125;
                    let lbs = Math.floor(oz / 14);
                    oz = Math.floor(oz - lbs * 14);
                    return lbs.toString() + " lbs" + (oz ? " " + oz.toString() + " oz" : "");
                }
                else if (g < 508023.5) { // Less than half a ton
                    return Math.floor(g / 453.59237).toString() + " lbs";
                }
                else if (g < 10160470) { // Less than 10 ton
                    return (Math.floor(g * 10 / 1016047) / 10).toString() + " ton";
                }
                else {
                    return Math.floor(g / 1016047).toString() + " ton";
                }
            }
        }

        class WordyRegex extends UnitRegex {
            static readonly number_words: string[] = [
                "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"
            ];
            static readonly fraction_chars: string[] = [
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
            static readonly fraction_words: string[] = [
                "quarter",
                "third",
                "half"
            ];
            static readonly value: string =
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
                super(new RegExp(WordyRegex.value + wsp + exp, "i"));
                this.conv = conv;
            }

            process(values: (string | number)[]): string | null {
                for (let index = 1; index < values.length; index++) {
                    let value = values[index].toString();
                    let i = WordyRegex.number_words.indexOf(value);
                    if (i !== -1) {
                        values[index] = i + 1;
                        continue;
                    }
                    i = WordyRegex.fraction_chars.indexOf(value);
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
                        values[index] = v;
                        continue;
                    }
                    i = WordyRegex.fraction_words.indexOf(value);
                    if (i !== -1) {
                        let v: number;
                        if (i === 0) {
                            v = 1 / 4;
                        }
                        else if (i === 1) {
                            v = 1 / 3;
                        }
                        else if (i === 2) {
                            v = 1 / 2;
                        }
                        else {
                            return null;
                        }
                        values[index] = v;
                        continue;
                    }
                    let split = value.split('/');
                    if (split === null || split.length !== 2) {
                        return null;
                    }
                    values[index] = toFloat(split[0]) / toFloat(split[1]);
                }

                return this.conv.process(values);
            }
        }


        let convs: UnitRegex[] = [];

        function createRegExps<T extends MultiUnitRegex>(c: { new (regex: RegExp | null, groups: string[]): T; units: UnitType[]; }): void {
            let exp = "";
            let units: string[] = [];
            for (let s = 0; s < c.units.length; s++) {
                exp += "(?:" + ws + valueExp + ws + c.units[s].exp + ")?";
                units.push(c.units[s].name);
                convs.push(new WordyRegex(c.units[s].exp, new c(null, [c.units[s].name])));
            }
            convs.push(new c(new RegExp("(?=\\d)" + exp + "(?!\\d)", "i"), units));
        }

        if (Settings.unitConv.dist === UnitConvSetting.USToMetric) createRegExps(DistanceUStoM);
        else if (Settings.unitConv.dist === UnitConvSetting.MetricToUS) createRegExps(DistanceMtoUS);

        if (Settings.unitConv.volume === UnitConvSetting.USToMetric) createRegExps(VolumeUStoM);
        else if (Settings.unitConv.volume === UnitConvSetting.MetricToUS) createRegExps(VolumeMtoUS);

        if (Settings.unitConv.weight  === UnitConvSetting.USToMetric) createRegExps(WeightUStoM);
        else if (Settings.unitConv.weight === UnitConvSetting.MetricToUS) createRegExps(WeightMtoUS);

        if (Settings.unitConv.temp  === UnitConvSetting.USToMetric)
            convs.push(new TemperatureFtoC(new RegExp(valueNegExp + "(?:" + ws + "(?:degree|degrees|fahrenheit|F|\xB0))+((?:ws+(?:below|below zero))?)", "i")));
        else if (Settings.unitConv.temp === UnitConvSetting.MetricToUS)
            convs.push(new TemperatureCtoF(new RegExp(valueNegExp + "(?:" + ws + "(?:degree|degrees|celcius|centigrade|C|\xB0))+((?:ws+(?:below|below zero))?)", "i")));

        if (convs.length !== 0) {

            let all_regex = new RegExp('(^|\\b|\\.|,|\\s|"|\')((?:' + convs.join(')|(?:').replace(/\((?!\?)/g, "(?:") + '))(?=\\b|\\.|,|\\s|"|\'|$)', 'gi');

            function ignoreElement(elem: Element) {
                if (elem.tagName === "SPAN" && elem.hasAttribute("class")) {
                    let classes = (elem.getAttribute("class") || "").split(" ");
                    if (classes.indexOf("mw_ao3track_unitconv") !== -1) {
                        return true;
                    }
                }
                if (elem.tagName === "LINK" || elem.tagName === "SCRIPT" || elem.tagName === "HEAD" || elem.tagName === "SELECT" || elem.tagName === "INPUT" || elem.tagName === "TEXTAREA" || elem.getAttribute("contenteditable") === "true") {
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
                } else if (node.nodeType === 1) {
                    if (ignoreElement(node as Element)) {
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
                    let text = split[s] + (s + 1 < split.length ? split[s + 1] : "");
                    if (text !== '') {
                        let textnode = document.createTextNode(text);
                        fragment.appendChild(textnode);
                    }
                    s += 2;
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
}