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
    export namespace Data {

        export enum Ao3PageType {
            Tag,
            Search,
            Work,
            Bookmarks,
            Other,
            Unknown
        }

        export enum Ao3TagType {
            Warnings,
            Rating,
            Category,
            Fandoms,
            Relationships,
            Characters,
            Freeforms,
            Other
        }

        export enum Ao3RequiredTag {
            Rating,
            Warnings,
            Category,
            Complete
        }

        export class Ao3WorkDetails
        {
            constructor()
            {
            }

            WorkId: number;
            Authors: Map<string,string>;
            Recipiants: Map<string, string>;
            Series : Map<string,[number,string]>;
            LastUpdated :string;
            Words? : number;
            Chapters : [number|null, number, number|null];
            Collections? : number;
            Comments? : number;
            Kudos? : number;
            Bookmarks? : number;
            Hits? : number;
            Summary : string;
        }

        export class Ao3PageModel {
            constructor(uri: URL)
            {
                this.Uri = uri;
                this.Title = "";
                this.Type =  Ao3PageType.Unknown;
            }
            Uri: URL;
            Type: Ao3PageType;
            PrimaryTag?: string;
            PrimaryTagType?: Ao3TagType;
            Title: string;

            Tags?: Map<Ao3TagType,string[]>;
            RequiredTags?: Map<Ao3RequiredTag, [string, string]|null>;

            Details?: Ao3WorkDetails;
            Language?: string;
            SearchQuery?: string;       

            GetRequiredTagUri(tag: Ao3RequiredTag) {
                let rt: [string, string] | null = null;

                if (this.RequiredTags) {
                    rt = this.RequiredTags.get(tag) || null;
                }

                if (rt === null) {
                    if (tag === Ao3RequiredTag.Category) { rt = ["category-none", "None"]; }
                    else if (tag === Ao3RequiredTag.Complete) { rt = ["category-none", "None"]; }
                    else if (tag === Ao3RequiredTag.Rating) { rt = ["rating-notrated", "None"]; }
                    else if (tag === Ao3RequiredTag.Warnings) { rt = ["warning-none", "None"]; }
                    else { rt = ["warning-none", "None"]; }
                }
                return new URL("http://archiveofourown.org/images/skins/iconsets/default_large/" + rt[0] + ".png");
            }

            GetRequiredTagText(tag: Ao3RequiredTag) : string
            {
                let rt: [string, string] | null = null;

                if (this.RequiredTags) {
                    rt = this.RequiredTags.get(tag) || null;
                }
                if (rt === null) {
                    return "";
                }

                return rt[1];
            }     

        }
    }
}