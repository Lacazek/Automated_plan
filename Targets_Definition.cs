using Structure_optimisation;
using System.Collections.Generic;


//***************************************************************
//
// Cette classe permet d'alimenter les Text bloc pour guider l'utilisateur dans le choix des volumes
// Elle est static afin d'être accessible de n'importe où

namespace Opti_Struct
{
    static class Targets_Definition
    {
        private static List<string[]> _targets;

        internal static List<string[]> Targets
        {
            get { return _targets; }
        }
        static Targets_Definition()
        {
            _targets = new List<string[]>();
        }

        internal static void FillTargets(UserInterfaceModel model, string index)
        {
            _targets.Clear();

            // sein
            if (index.ToLower().Contains("sein"))
            {
                if (index.ToLower().Contains("seul"))
                    _targets.Add(new[] { "CTV sein" });
                else if (index.ToLower().Contains("complet") && index.ToLower().Contains("boost"))
                    _targets.Add(new[] { "CTV sein", "CTV CMI", "CTV Sus Clav", "CTV Sous Clav", "CTV Axillaire", "CTV Boost" });
                else if (index.ToLower().Contains("boost"))
                    _targets.Add(new[] { "CTV sein", "CTV Boost" });
                else if (index.ToLower().Contains("complet"))
                    _targets.Add(new[] { "CTV sein", "CTV CMI", "CTV Sus Clav", "CTV Sous Clav", "CTV Axillaire" });
            }

            // ORL
            if (index.ToLower().Contains("orl"))
            {
                if (index.ToLower().Contains("2niveaux"))
                    _targets.Add(new[] { "PTV HR", "PTV BR", "GTV +" });
                else if (index.ToLower().Contains("3niveaux"))
                    _targets.Add(new[] { "PTV HR", "PTV RI", "PTV BR", "GTV +", "GTV N +" });
            }

            // Poumons
            if (index.ToLower().Contains("poumon"))
            {
                if (index.ToLower().Contains("4d"))
                    _targets.Add(new[] { "PTV", "ITV" });
                else
                    _targets.Add(new[] { "PTV" });
            }

            //Prostate
            if (index.ToLower().Contains("prostate"))
            {
                if (index.ToLower().Contains("aire"))
                    _targets.Add(new[] { "PTV 2", "PTV 1", "PTV N" });
                else if (index.ToLower().Contains("gg"))
                    _targets.Add(new[] { "PTV 2", "PTV 1", "PTV N", "PTV GG" });
                else if (index.ToLower().Contains("daz"))
                    _targets.Add(new[] { "prostate", "VS" });
                else if (index.ToLower().Contains("aire") && index.ToLower().Contains("daz"))
                    _targets.Add(new[] { "prostate", "VS", "CTV N" });
                else if (index.ToLower().Contains("gg") && index.ToLower().Contains("daz"))
                    _targets.Add(new[] { "prostate", "VS", "CTV N", "CTV GG" });
                else
                    _targets.Add(new[] { "PTV 2", "PTV 1" });
            }

            //Loge
            if (index.ToLower().Contains("loge"))
            {
                _targets.Add(new[] { "PTV 2", "PTV 1" });
            }

            //Rectum
            if (index.ToLower().Trim().Contains("canalanal"))
            {
                if (index.ToLower().Contains("boost"))
                    _targets.Add(new[] { "PTV 1", "PTV 2" });
                else
                    _targets.Add(new[] { "PTV 1" });
            }

            //Col uterin
            if (index.ToLower().Contains("pelvis"))
            {
                if (index.ToLower().Contains("gg"))
                    _targets.Add(new[] { "PTV T", "PTV N" });
                else
                    _targets.Add(new[] { "PTV T" });
            }

            //Encephale
            if (index.ToLower().Contains("encephale"))
            {
                if (index.ToLower().Contains("gg"))
                    _targets.Add(new[] { "PTV BR", " PTV HR" });
                else
                    _targets.Add(new[] { "PTV" });
            }

            // Stereo
            if (index.ToLower().Contains("stereo"))
            {
                if (index.ToLower().Contains("4d"))
                    _targets.Add(new[] { "PTV", "ITV" });
                else
                    _targets.Add(new[] { "PTV" });
            }
        }
    }
}
