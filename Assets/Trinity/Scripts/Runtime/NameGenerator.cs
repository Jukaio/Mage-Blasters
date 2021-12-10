using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>NameGenerator</c> generates names from a random animal and adjective.
    /// </summary>
    public class NameGenerator : MonoBehaviour
    {
        [HideInInspector]
        public string Name;

        // Adjectives
        private string[] Adjectives = {
            "adorable","adventurous","aggressive","agreeable","angry","annoying","anxious","arrogant",
            "attractive","average","awful","bad","beautiful","bewildered","black","bloody","blue","blue-eyed",
            "blushing","bored","brainy","brave","bright","busy","careful","cautious","charming","cheerful","clever",
            "cloudy","clumsy","colorful","combative","comfortable","confused","cooperative","courageous","crazy",
            "creepy","cruel","curious","cute","dangerous","dark","dead","defeated","defiant","delightful","depressed",
            "determined","disgusted","disturbed","dizzy","doubtful","dull","eager","elegant","embarrassed","enchanting",
            "encouraging","energetic","enthusiastic","evil","excited","exuberant","fair","faithful","famous","fancy",
            "fantastic","fierce","foolish","fragile","frail","frantic","friendly","frightened","funny","gentle","gifted",
            "glamorous","gleaming","glorious","good","gorgeous","graceful","grieving","grotesque","grumpy","handsome",
            "happy","healthy","helpful","helpless","hilarious","homeless","homely","horrible","hungry","impossible",
            "innocent","inquisitive","itchy","jealous","jolly","lazy","lively","lonely","lovely","lucky","magnificent",
            "misty","muddy","mushy","mysterious","nasty","naughty","nervous","nice","nutty","obedient","obnoxious",
            "odd","old-fashioned","open","outrageous","outstanding","panicky","plain","pleasant","poor","powerful",
            "precious","prickly","proud","putrid","puzzled","real","relieved","repulsive","rich","scary","selfish",
            "shiny","shy","silly","sleepy","smiling","smoggy","sore","sparkling","splendid","spotless","stormy",
            "strange","stupid","successful","super","talented","tame","tasty","tender","tense","terrible","thankful",
            "thoughtful","thoughtless","tired","tough","troubled","ugliest","ugly","unsightly","unusual","upset",
            "uptight","victorious","vivacious","wandering","weary","wicked","wide-eyed","wild","witty","worried",
            "worrisome","zany","zealous"
        };

        // Nouns
        private string[] Nouns = {
            "Albatross","Alligator","Alpaca","Ant","Baboon","Barracuda",
            "Bat","Beagle","Bear","Beatle","Bird","Buffalo","Bulldog","Caiman","Camel","Cat","Cheetah",
            "Chicken","Chimpanzee","Chinchilla","Cikada","Collie","Cow","Coyote","Crab","Crocodile",
            "Dalmatien","Deer","Dingo","Dog","Dolphin","Donkey","Duck","Eagle","Eel","Elephant","Elk",
            "Emu","Falcon","Fish","Flamingo","Flounder","Fly","Fox","Frog","Gecko","Gibbon","Goat",
            "Goose","Guppy","Hamster","Hare","Herring","Horse","Human","Hyena","Ibis","Impala","Jackal",
            "Jaguar","Jellyfish","Kangaroo","Kiwi","Koala","Lemu","Leopard","Lion","Lizard","Llama","Lobster",
            "Lynx","Mandrill","Mastiff","Mink","Monkey","Moose","Moth","Mouse","Mule","Muskox","Narwhal","Ocelot",
            "Octopus","Okapi","Opossum","Orangutan","Otter","Oyster","Panther","Parrot","Peacock","Pelican","Pig",
            "Pigeon","Piranha","Poodle","Rabbit","Raccoon","Ragdoll","Rat","Reindeer","Rhino","Rottweiler","Salamander",
            "Salmon","Sawfish","Scorpion","Seal","Shark","Sheep","Shrimp","Skunk","Snail","Sparrow","Sponge","Squid",
            "Squirrel","Starfish","Stingray","Swan","Tapir","Termite","Terrier","Tiger","Tuna","Vulture","Walrus",
            "Warthog","Wolf","Wolverine","Yak","Zebra"
        };

        private string NameKey = "ConnectUserName";

        private void Awake()
        {
            if (PlayerPrefs.HasKey(NameKey))
                Name = PlayerPrefs.GetString(NameKey);
            else
                Generate();
        }

        public void Generate()
        {
            if ((Adjectives.Length > 0) && (Nouns.Length > 0))
            {
                string a = Adjectives[Random.Range(0, Adjectives.Length)];
                string b = Nouns[Random.Range(0, Nouns.Length)];

                Name = (char.ToUpper(a[0]) + a.Substring(1)) + (char.ToUpper(b[0]) + b.Substring(1));
                PlayerPrefs.SetString(NameKey, Name);
            }
            else
            {
                Name = "AnonymousBird";
            }
        }
    }
}



