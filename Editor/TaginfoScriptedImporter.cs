using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.CV.SyntheticHumans.Tags;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.CV.SyntheticHumans.Editor
{
    [ScriptedImporter(1, "taginfo")]
    class TaginfoScriptedImporter : ScriptedImporter
    {
        static Dictionary<string, Type> tagTypeMapping = new Dictionary<string, Type>()
        {
            { "vat", typeof(VATTag) },
            { "bodymesh", typeof(BodyTag) },
            { "hairmesh", typeof(HairTag) },
            { "clothingmesh", typeof(ClothingTag) },
        };

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var text = File.ReadAllText(ctx.assetPath);
            var asDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);

            Assert.IsTrue(asDict.ContainsKey("tagtype"), $".taginfo {ctx.assetPath} does not contain a 'tagtype' field");
            var tagTypeName = (string)asDict["tagtype"];
            Assert.IsTrue(tagTypeMapping.ContainsKey(tagTypeName), $"Unknown tagtype {tagTypeName} of .taginfo {ctx.assetPath}");
            var tag = (SyntheticHumanTag)ScriptableObject.CreateInstance(tagTypeMapping[tagTypeName]);

            try
            {
                tag.linkedAsset = FindLinkedAsset(ctx.assetPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not find linked asset for tag {ctx.assetPath}: {e}");
            }

            // NOTE (July 28, 2022): there used to be a section here for importing json-based weight files, but it was removed. The issue
            // was that this import relied on paths relative to the asset pack, which would cause import failures if the taginfo
            // was imported before its respective asset pack. Because the json weight files were not being used at the time, we decided
            // to just entirely remove this feature. To find its previous implementation, search for commits on this date.

            JsonConvert.PopulateObject(text, tag);
            tag.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            ctx.AddObjectToAsset("Tag", tag);
            ctx.SetMainObject(tag);

            // TODO need to figure out how to safely update asset pack so it handles deletions too
            // MK: RE above todo: I added logic in PalletAssetImporter and AssetPackManager to keep packs updated with additions, deletions, and moves of packable assets and synthetic human tags.
        }

        static Object FindLinkedAsset(string tagPath)
        {
            var baseName = Path.GetFileNameWithoutExtension(tagPath);

            // Search for only things of the same name in the same folder as the tag, but not the tag itself
            var candidatePaths =
                AssetDatabase.FindAssets(baseName, new string[] { Path.GetDirectoryName(tagPath) })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path != tagPath);

            Assert.IsTrue(candidatePaths.Any(), "No linked asset found");
            Assert.IsFalse(candidatePaths.Count() > 1, $"{candidatePaths.Count()} linked assets found. Only 1 expected.");

            return AssetDatabase.LoadAssetAtPath<Object>(candidatePaths.First());
        }
    }
}
