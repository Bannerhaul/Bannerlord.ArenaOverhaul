using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

using JetBrains.Annotations;

using System.Collections.Generic;
using System.Xml;

using TaleWorlds.ModuleManager;

namespace ArenaOverhaul.ViewModelMixin
{
    
    [PrefabExtension("ArenaPracticeFight", "descendant::ListPanel[@IsVisible='@IsPlayerPracticing']")]
    [UsedImplicitly]
    internal sealed class ArenaPracticeFightPrefabExtension : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes =>
        [
            new Attribute( "IsVisible", "@IsStandardPanelVisible" ),
            new Attribute( "Id", "StandardPanel" ),
        ];
    }
    
    [PrefabExtension("ArenaPracticeFight", "descendant::ListPanel[@Id='StandardPanel']")]
    [UsedImplicitly]
    internal sealed class ArenaPracticeFightPrefabInsertExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        private readonly XmlDocument _document;

        public ArenaPracticeFightPrefabInsertExtension()
        {
            _document = new XmlDocument();
            _document.Load(ModuleHelper.GetModuleFullPath("ArenaOverhaul") + "GUI/PrefabExtensions/ArenaPracticeFightInjection.xml");
        }

        [PrefabExtensionXmlDocument]
        [UsedImplicitly]
        public XmlDocument GetPrefabExtension() => _document;
    }
}