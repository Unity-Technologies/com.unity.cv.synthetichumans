using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Tags
{
    [Serializable]
    public abstract class SyntheticHumanTag : ScriptableObject
    {
        public const string FOLDER_TAG_IDENTIFIER = "FolderTag";

        public UnityEngine.Object linkedAsset;


        /// <summary>
        /// Copy non-zero, non-null, non "" values from a template SyntheticHumanTag to this SyntheticHumanTag.
        /// </summary>
        /// <param name="template">The template tag to copy from</param>
        /// <returns>Returns true if fields were overridden by template.</returns>
        public bool SetFieldsFromTemplate(SyntheticHumanTag template)
        {
            // REFERENCE:  https://forum.unity.com/threads/reflection-getvalue-setvalue-on-scripts-unityscript.211125/

            if (template == null)
            {
                return false;
            }

            var dirty = false;
            var templateType = template.GetType();

            foreach (var fieldInfo in templateType.GetFields())
            {
                var fieldInfoName = fieldInfo.Name;
                var value = fieldInfo.GetValue(template);

                try
                {
                    if (value != null && value.ToString() != "None" && value.ToString() != "")
                    {
                        templateType.GetField(fieldInfoName).SetValue(this, value);
                        dirty = true;
                    }
                }
                catch (UnassignedReferenceException)
                {
                    // This means that the value on the template was never assigned in the first place, which we treat
                    // the same as an assigned value of null by ignoring it.
                }
            }

            return dirty;
        }
    }
}
