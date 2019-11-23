using System.Collections;
using System.Collections.Generic;

namespace Content.Shared.Preferences.Appearance
{
    public static class HairStyles
    {
        public const string DefaultHairStyle = "Bald";
        public const string DefaultFacialHairStyle = "Shaved";

        public static readonly Dictionary<string, string> HairStylesMap = new Dictionary<string, string>
        {
            {"Bald", "bald"},
            {"Afro", "afro"},
            {"Big Afro", "bigafro"},
            {"Afro 2", "afro2"},
            {"Asymmetrical Bob", "asymmbob"},
            {"Balding Hair", "balding"},
            {"Bedhead", "bedhead"},
            {"Bedhead 2", "bedheadv2"},
            {"Bedhead 3", "bedheadv3"},
            {"Beehive", "beehive"},
            {"Beehive 2", "beehive2"},
            {"Birdnest", "birdnest"},
            {"Birdnest 2", "birdnest2"},
            {"Mercenary", "blackswordsman"},
            {"Bob", "bobcut"},
            {"Bobcurl", "bobcurl"},
            {"Bowl 1", "bowlcut1"},
            {"Bowl 2", "bowlcut2"},
            {"Floorlength Braid", "braid"},
            {"Long Braid", "hbraid"},
            {"Business Hair", "business"},
            {"Business Hair 2", "business2"},
            {"Business Hair 3", "business3"},
            {"Business Hair 4", "business4"},
            {"Bun", "bun"},
            {"Casual Bun", "bunalt"},
            {"Bun 2", "bun2"},
            {"Bun 3", "bun3"},
            {"Buzzcut", "buzzcut"},
            {"Chop", "chop"},
            {"CIA", "cia"},
            {"Combover", "combover"},
            {"Coffee House", "coffeehouse"},
            {"Crewcut", "crewcut"},
            {"Chrono", "toriyama"},
            {"Curls", "curls"},
            {"Cut Hair", "cuthair"},
            {"Dandy Pompadour", "dandypompadour"},
            {"Devil Lock", "devilock"},
            {"Double-Bun", "doublebun"},
            {"Dreadlocks", "dreads"},
            {"80's", "80s"},
            {"Emo", "emo"},
            {"Flow Hair", "flowhair"},
            {"The Family Man", "thefamilyman"},
            {"Father", "father"},
            {"Feather", "feather"},
            {"Cut Hair Alt", "femc"},
            {"Flaired Hair", "flair"},
            {"Emo Fringe", "emofringe"},
            {"Fringetail", "fringetail"},
            {"Gelled Back", "gelled"},
            {"Gentle", "gentle"},
            {"Half-banged Hair", "halfbang"},
            {"Half-banged Hair Alt", "halfbang_alt"},
            {"Half-Shaved", "halfshaved"},
            {"Half-Shaved Emo", "halfshaved_emo"},
            {"Hamaski Hair", "hamasaki"},
            {"Combed Hair", "hbangs"},
            {"Combed Hair Alt", "hbangs_alt"},
            {"High Ponytail", "highponytail"},
            {"Hime Cut", "himecut"},
            {"Hime Cut Alt", "himecut_alt"},
            {"Hitop", "hitop"},
            {"Adam Jensen Hair", "jensen"},
            {"Joestar", "joestar"},
            {"Pigtails", "kagami"},
            {"Kare", "kare"},
            {"Kusanagi Hair", "kusanagi"},
            {"Ladylike", "ladylike"},
            {"Ladylike alt", "ladylike2"},
            {"Long Emo", "emolong"},
            {"Long Hair", "vlong"},
            {"Long Hair Alt", "longeralt2"},
            {"Very Long Hair", "longest"},
            {"Longer Fringe", "vlongfringe"},
            {"Long Fringe", "longfringe"},
            {"Overeye Long", "longovereye"},
            {"Man Bun", "manbun"},
            {"Drillruru", "drillruru"},
            {"Medium Braid", "shortbraid"},
            {"Medium Braid Alt", "mediumbraid"},
            {"Messy Bun", "messybun"},
            {"Modern", "modern"},
            {"Mohawk", "mohawk"},
            {"Mulder", "mulder"},
            {"Nia", "nia"},
            {"Nitori", "nitori"},
            {"Odango", "odango"},
            {"Ombre", "ombre"},
            {"Oxton", "oxton"},
            {"Parted", "parted"},
            {"Pixie", "pixie"},
            {"Pompadour", "pompadour"},
            {"Ponytail 1", "ponytail"},
            {"Ponytail 2", "ponytail2"},
            {"Ponytail 3", "ponytail3"},
            {"Ponytail 4", "ponytail4"},
            {"Ponytail 5", "ponytail5"},
            {"Ponytail 6", "ponytail6"},
            {"Ponytail 7", "ponytail7"},
            {"Poofy", "poofy"},
            {"Poofy Alt", "poofy2"},
            {"Quiff", "quiff"},
            {"Ramona", "ramona"},
            {"Reverse Mohawk", "reversemohawk"},
            {"Ronin", "ronin"},
            {"Rows", "rows1"},
            {"Rows Alt", "rows2"},
            {"Rows Bun", "rows3"},
            {"Flat Top", "sargeant"},
            {"Scully", "scully"},
            {"Shaved Mohawk", "shavedmohawk"},
            {"Shaved Part", "shavedpart"},
            {"Short Hair", "short"},
            {"Short Hair 2", "short2"},
            {"Short Hair 3", "short3"},
            {"Short Bangs", "shortbangs"},
            {"Overeye Short", "shortovereye"},
            {"Shoulder-length Hair", "shoulderlen"},
            {"Sidepart Hair", "sidepart"},
            {"Side Ponytail", "stail"},
            {"One Shoulder", "oneshoulder"},
            {"Tress Shoulder", "tressshoulder"},
            {"Side Ponytail 2", "ponytailf"},
            {"Side Swipe", "sideswipe"},
            {"Skinhead", "skinhead"},
            {"Messy Hair", "smessy"},
            {"Sleeze", "sleeze"},
            {"Spiky", "spikey"},
            {"Stylo", "stylo"},
            {"Spiky Ponytail", "spikyponytail"},
            {"Top Knot", "topknot"},
            {"Thinning", "thinning"},
            {"Thinning Rear", "thinningrear"},
            {"Thinning Front", "thinningfront"},
            {"Undercut", "undercut"},
            {"Unkept", "unkept"},
            {"Updo", "updo"},
            {"Vegeta", "toriyama2"},
            {"Overeye Very Short", "veryshortovereye"},
            {"Overeye Very Short, Alternate", "veryshortovereyealternate"},
            {"Volaju", "volaju"},
            {"Wisp", "wisp"},
            {"Zieglertail", "ziegler"},
            {"Zone Braid", "zone"},
        };

        public static readonly Dictionary<string, string> FacialHairStylesMap = new Dictionary<string, string>()
        {
            {"Shaved", "shaved"},
            {"Watson Mustache", "watson"},
            {"Hulk Hogan Mustache", "hogan"},
            {"Van Dyke Mustache", "vandyke"},
            {"Square Mustache", "chaplin"},
            {"Selleck Mustache", "selleck"},
            {"Neckbeard", "neckbeard"},
            {"Full Beard", "fullbeard"},
            {"Long Beard", "longbeard"},
            {"Very Long Beard", "wise"},
            {"Elvis Sideburns", "elvis"},
            {"Abraham Lincoln Beard", "abe"},
            {"Chinstrap", "chin"},
            {"Hipster Beard", "hip"},
            {"Goatee", "gt"},
            {"Adam Jensen Beard", "jensen"},
            {"Volaju", "volaju"},
            {"Dwarf Beard", "dwarf"},
            {"3 O'clock Shadow", "3oclock"},
            {"3 O'clock Shadow and Moustache", "3oclockmoustache"},
            {"5 O'clock Shadow", "5oclock"},
            {"5 O'clock Shadow and Moustache", "5oclockmoustache"},
            {"7 O'clock Shadow", "7oclock"},
            {"7 O'clock Shadow and Moustache", "7oclockmoustache"},
            {"Mutton Chops", "mutton"},
            {"Mutton Chops and Moustache", "muttonmu"},
            {"Walrus Moustache", "walrus"},
        };
    }
}
