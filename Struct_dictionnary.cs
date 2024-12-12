/******************************************************************************
 * Nom du fichier : Struct_dictionnary.cs
 * Auteur         : LACAZE Killian
 * Date de création : [02/10/2024]
 * Description    : [Brève description du contenu ou de l'objectif du code]
 *
 * Droits d'auteur © [2024], [LACAZE.K].
 * Tous droits réservés.
 * 
 * Ce code a été développé exclusivement par LACAZE Killian. Toute utilisation de ce code 
 * est soumise aux conditions suivantes :
 * 
 * 1. L'utilisation de ce code est autorisée uniquement à titre personnel ou professionnel, 
 *    mais sans modification de son contenu.
 * 2. Toute redistribution, copie, ou publication de ce code sans l'accord explicite 
 *    de l'auteur est strictement interdite.
 * 3. L'auteur assume la responsabilité de l'utilisation de ce code dans ses propres projets.
 * 
 * CE CODE EST FOURNI "EN L'ÉTAT", SANS AUCUNE GARANTIE, EXPRESSE OU IMPLICITE. 
 * L'AUTEUR DÉCLINE TOUTE RESPONSABILITÉ POUR TOUT DOMMAGE OU PERTE RÉSULTANT 
 * DE L'UTILISATION DE CE CODE.
 *
 * Toute utilisation non autorisée ou attribution incorrecte de ce code est interdite.
 ******************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;

namespace Opti_Struct
{
    internal class Struct_dictionnary
    {
        private readonly Dictionary<string, string> _allStruct;
        public event EventHandler<string> MessageChanged;
        private StructureSet _ss;
        private string _message;

        public Struct_dictionnary(StructureSet ss)
        {
            _allStruct = new Dictionary<string, string>();
            _ss = ss;
            CreateDictionnary();
        }

        internal string SearchStruct(string name)
        {
            double maxSimilarity = 0;
            string ClosestMatch = name;
            double similarity;
            List<string> _multipleStruct = new List<string>();

            try
            {
                Message = $"Liste des structures";
                foreach (var keys in _allStruct.Keys)
                {
                    foreach (var candidate in _ss.Structures)
                    {
                        if (Regex.IsMatch(name, keys, RegexOptions.IgnoreCase) && Regex.IsMatch(candidate.Id, _allStruct[keys], RegexOptions.IgnoreCase))
                        {
                            if (Regex.IsMatch(candidate.Id, keys, RegexOptions.IgnoreCase))
                            {
                                if (!_multipleStruct.Contains(candidate.Id))
                                {
                                    _multipleStruct.Add(candidate.Id);
                                    Message = $"{candidate.Id}";
                                }
                                ClosestMatch = candidate.Id;
                            }
                        }
                    }
                }

                if (_multipleStruct.Count > 1)
                {
                    Message = $"\nIl existe {_multipleStruct.Count} structures respectant la REGEX";
                    foreach (var st in _multipleStruct)
                    {
                        similarity = CalculateCombinedSimilarity(Regex.Replace(st.ToLower(), @"[\s\r\n]+", "").ToLower().Trim(), Regex.Replace(name.ToLower(), @"[\s\r\n]+", "").ToLower().Trim());
                        if (similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            ClosestMatch = st;
                        }
                        Message = $"Test de la structure {ClosestMatch} ; Indice de similarité : {maxSimilarity}";
                    }
                }
                Message = $"La structure {name}a été corrigée en {ClosestMatch}\n";
                _multipleStruct.Clear();
                return ClosestMatch;
            }
            catch (Exception ex)
            {
                Message = $"Erreur {ex.Message}\n";
                return ClosestMatch;
            }
        }

        #region Calcul des similarités
        #region Calcul of similarity (Distance de Levenshtein)
        internal double CalculateLevenshteinSimilarity(string name, string key)
        {

            int n = name.Length;
            int m = key.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return 0;
            if (m == 0) return 0;

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (key[j - 1] == name[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            int levenshteinDistance = d[n, m];
            int maxLength = Math.Max(n, m);
            return 1.0 - (double)levenshteinDistance / maxLength;
        }
        #endregion

        #region Calcul of cosinus similarity
        public double CalculateCosineSimilarity(string name, string key)
        {
            var vec1 = GetCharacterFrequencyVector(name);
            var vec2 = GetCharacterFrequencyVector(key);

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            foreach (var kvp in vec1)
            {
                int value2;
                if (vec2.TryGetValue(kvp.Key, out value2))
                {
                    dotProduct += kvp.Value * value2;
                }
                magnitude1 += kvp.Value * kvp.Value;
            }

            foreach (var kvp in vec2)
            {
                magnitude2 += kvp.Value * kvp.Value;
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }


        private Dictionary<char, int> GetCharacterFrequencyVector(string str)
        {
            var frequency = new Dictionary<char, int>();
            foreach (var c in str)
            {
                if (frequency.ContainsKey(c))
                {
                    frequency[c]++;
                }
                else
                {
                    frequency[c] = 1;
                }
            }
            return frequency;
        }
        #endregion

        public double CalculateCombinedSimilarity(string name, string key)
        {
            double levenshteinSim = CalculateLevenshteinSimilarity(name, key);
            double cosineSim = CalculateCosineSimilarity(name, key);

            // Combiner les deux mesures, vous pouvez ajuster les pondérations selon vos besoins
            return (levenshteinSim + cosineSim) / 2.0;
        }
        #endregion

        #region Get and Set
        internal StructureSet ss
        {
            get { return _ss; }
            set
            {
                _ss = value;
            }
        }
        internal string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnMessageChanged();
            }
        }
        protected virtual void OnMessageChanged()
        {
            MessageChanged?.Invoke(this, _message);
        }
        #endregion

        #region Create dictionnary
        private void CreateDictionnary()
        {

            ///////////////////////////////////////////////////////////////////////////// GTV /////////////////////////////////////////////////////////////////////////////
//OK            // ORL
            _allStruct.Add(@"(?i)\bg\s*t\s*v\s*[_\s]*n\s*[_\s]*(?:\+|\s*\+)?", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)g\s*t\s*v\s*[_\s]*n\s*[_\s]*(?:\+|\s*\+)?", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bg\s*t\s*v\s*[_\s]*b\s*r*[_\s]*(?:\s*\*\s*[1|2]|\s*\b)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bg\s*t\s*v\s*[_\s]*b\s*r*[_\s]*(?:\s*\*\s*[1|2]|\s*\b)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bg\s*t\s*v\s*[_\s]*h\s*r*[_\s]*(?:\s*\*\s*[1|2]|\s*\b)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bg\s*t\s*v\s*[_\s]*h\s*r*[_\s]*(?:\s*\*\s*[1|2]|\s*\b)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bg\s*t\s*v\s*[_\s]*n\s*[_\s]*g*[_\s]*(?:\s*1|\s*2|\s*)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)g\s*t\s*v\s*[_\s]*n\s*[_\s]*g*[_\s]*(?:\s*1|\s*2|\s*)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bg\s*t\s*v\s*[_\s]*n\s*[_\s]*d*[_\s]*(?:\s*1|\s*2|\s*)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)g\s*t\s*v\s*[_\s]*n\s*[_\s]*d*[_\s]*(?:\s*1|\s*2|\s*)", RegexOptions.IgnoreCase))?.Id ?? "");

            ///////////////////////////////////////////////////////////////////////////// CTV /////////////////////////////////////////////////////////////////////////////
//OK          // Sein

           _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[_\s]*c\s*m\s*i\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)c\s*t\s*v\s*[_\s]*c\s*m\s*i\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
           _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[_\s]*s\s*e\s*i\s*n\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)c\s*t\s*v\s*[_\s]*s\s*e\s*i\s*n\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
           _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*o\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)c\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*o\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
           _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)c\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
           _allStruct.Add(@"(?i)(?:\bc\s*t\s*v\s*[_\s]*l\s*i\s*t\s*u\s*m(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*u\s*t\s*o\s*m\s*r\s*a\s*l\b|c\s*t\s*v\s*_*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b)",
               _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:c\s*t\s*v\s*_*l\s*i\s*t\s*u\s*m(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*u\s*t\s*o\s*m\s*r\s*a\s*l\b|c\s*t\s*v\s*_*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[_\s]*p\s*a\s*r\s*o\s*i*[_\s]*(?:d|g)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bc\s*t\s*v\s*[_\s]*p\s*a\s*r\s*o\s*i*[_\s]*(?:d|g)\b", RegexOptions.IgnoreCase))?.Id ?? "");

            ///////////////////////////////////////////////////////////////////////////// PTV /////////////////////////////////////////////////////////////////////////////
            // ORL

            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*b\s*r\s*[_\s]*r*[_\s]*(1|2)?\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bp\s*t\s*v\s*[_\s]*b\s*r\s*[_\s]*r*[_\s]*(1|2)?\b", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*i\s*r\s*[_\s]*r*[_\s]*(1|2)?\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bp\s*t\s*v\s*[_\s]*i\s*r\s*[_\s]*r*[_\s]*(1|2)?\b", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*h\s*r\s*[_\s]*r*[_\s]*(1|2)?\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bp\s*t\s*v\s*[_\s]*h\s*r\s*[_\s]*r*[_\s]*(1|2)?\b", RegexOptions.IgnoreCase))?.Id ?? "");

            // Sein
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*c\s*m\s*i\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*t\s*v\s*[_\s]*c\s*m\s*i\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*s\s*e\s*i\s*n\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*t\s*v\s*[_\s]*s\s*e\s*i\s*n\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*o\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*o\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*t\s*v\s*[_\s]*(?:n\s*[_\s]*)?(?:s\s*u\s*s\s*[_\s]*)c\s*l\s*a\s*v\s*[_\s]*(?:d|g)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)(?:\bp\s*t\s*v\s*[_\s]*l\s*i\s*t\s*u\s*m(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*u\s*t\s*o\s*m\s*r\s*a\s*l\b|c\s*t\s*v\s*_*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:p\s*t\s*v\s*_*l\s*i\s*t\s*u\s*m(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*u\s*t\s*o\s*m\s*r\s*a\s*l\b|c\s*t\s*v\s*_*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b|c\s*t\s*v\s*l\s*i\s*t\s*_*tu(?:\s*a\s*l)?(?:\s*f\s*b)?\b)", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*p\s*a\s*r\s*o\s*i*[_\s]*(?:d|g)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bp\s*t\s*v\s*[_\s]*p\s*a\s*r\s*o\s*i*[_\s]*(?:d|g)\b", RegexOptions.IgnoreCase))?.Id ?? "");


            // pelvis
            _allStruct.Add(@"(?i)(?:\bp\s*t\s*v\s*[_\s]*(?:t|n)(?:\s*[_\s]*[12])?)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:p\s*t\s*v\s*[_\s]*(?:t|n)(?:\s*[_\s]*[12])?)", RegexOptions.IgnoreCase))?.Id ?? "");

            // prostate
            _allStruct.Add(@"(?i)\bp\s*t\s*v\s*[_\s]*(?:1|2|g[_\s]*g|n)", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*t\s*v\s*[_\s]*(?:1|2|g[_\s]*g|n)", RegexOptions.IgnoreCase))?.Id ?? "");

            // Poumons et stéréo
            _allStruct.Add(@"(?i)\b(?:i|p)\s*t\s*v*[_\s]*(?:\s*(?:1|2|n))?", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:i|p)\s*t\s*v*[_\s]*(?:\s*(?:1|2|n))?", RegexOptions.IgnoreCase))?.Id ?? "");

            // Volume cible DAZ (nomenclature n'incluant pas CTV)
            _allStruct.Add(@"(?i)\bp\s*r\s*o\s*s\s*t\s*a\s*t\s*e\s\b", _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)p\s*r\s*o\s*s\s*t\s*a\s*t\s*e\s\b", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bv\s*s\b|\bv*e\s*s\s*i\s*c\s*u\s*l\s*e*[_\s]*s\s*e\s*m\s*i\s*n\s*a\s*l\s*e\s\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bv\s*s\b|\bv*e\s*s\s*i\s*c\s*u\s*l\s*e*[_\s]*s\s*e\s*m\s*i\s*n\s*a\s*l\s*e\s\b", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bc\s*t\s*v\s*[\s_\-]*l\s*o\s*g\s*e\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bc\s*t\s*v\s*[\s_\-]*l\s*o\s*g\s*e\b", RegexOptions.IgnoreCase))?.Id ?? "");


            ///////////////////////////////////////////////////////////////////////////// OAR (IA) /////////////////////////////////////////////////////////////////////////////
            // Ordre OAR ( CE - Vessie - Coeur - moelle - TC - parotide D - parotide G - oesophage - Trachée - NOG - NOD - Chiasma - Encephale - paroi - poumons -
            // poumon D - poumon G - pharynx - larynx - thyroide - sein d & g - foie

            //OK            //CE
            _allStruct.Add(@"(?i)\bc\s*o\s*n\s*t\s*o\s*u\s*r\s*\s*e\s*x\s*t\s*e\s*r\s*n\s*e\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bc\s*o\s*n\s*t\s*o\s*u\s*r\s*\s*e\s*x\s*t\s*e\s*r\s*n\s*e\b", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            //Vessie
            _allStruct.Add(@"(?i)(?:\bv\s*e\s*s\s*s\s*i\s*e|v\s*e\s*s\s*s\s*i\s*o|v\s*e\s*s\s*i\s*e|v\s*e\s*s\s*i\s*e\s*t|\s*o\s*a\s*r[_\s]*v\s*e\s*s\s*s\s*s\s*i\s*e\s\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:v\s*e\s*s\s*s\s*i\s*e|v\s*e\s*s\s*s\s*i\s*o|v\s*e\s*s\s*i\s*e|v\s*e\s*s\s*i\s*e\s*t|\s*o\s*a\s*r[_\s]*v\s*e\s*s\s*s\s*s\s*i\s*e\s\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK           //Coeur
            _allStruct.Add(@"(?i)(?:\bo\s*a\s*r[_\s]*c\s*o\s*e\s*u\s*r\b|\bc\s*o\s*e\s*u\s*r\b|\b(c\s*o\s*e\s*u\s*r\b|o\s*a\s*r\s*c\s*o\s*e\s*u\s*r)\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:o\s*a\s*r[_\s]*c\s*o\s*e\s*u\s*r|c\s*o\s*e\s*u\s*r|\b(c\s*o\s*e\s*u\s*r|o\s*a\s*r\s*c\s*o\s*e\s*u\s*r)\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK           // Moelle
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r\s*m\s*o\s*e\s*l\s*l\s*e|o\s*a\s*r[_\s]*m\s*o\s*e\s*l\s*l\s*e|\bmo\s*e\s*l\s*l\s*e\b|\bo\s*a\s*r\s*mo\s*e\s*l\s*l\s*e\b)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r\s*m\s*o\s*e\s*l\s*l\s*e|o\s*a\s*r[_\s]*m\s*o\s*e\s*l\s*l\s*e|\bmo\s*e\s*l\s*l\s*e\b|\bo\s*a\s*r\s*mo\s*e\s*l\s*l\s*e\b)\b", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // TC
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]*t\s*r\s*o\s*n\s*c[_\s]*c\s*e\s*r\s*e\s*b\s*r\s*a\s*l\b|\bt\s*r\s*o\s*n\s*c[_\s]*c\s*e\s*r\s*e\s*b\s*r\s*a\s*l\b|\bt[_\s]*c\b|o\s*a\s*r[_\s]*t[_\s]*c\b)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]*t\s*r\s*o\s*n\s*c[_\s]*c\s*e\s*r\s*e\s*b\s*r\s*a\s*l\b|\bt\s*r\s*o\s*n\s*c[_\s]*c\s*e\s*r\s*e\s*b\s*r\s*a\s*l\b|\bt[_\s]*c\b|o\s*a\s*r[_\s]*t[_\s]*c\b)\b", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // parotide D
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]*p\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*d\b|\bp\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*d\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]*p\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*d\b|\bp\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*d\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // parotide G
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]*p\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*g\b|\bp\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*g\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]*p\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*g\b|\bp\s*a\s*r\s*o\s*t\s*i\s*d\s*e[_\s]*g\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Oesophage
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]o\s*e\s*s\s*o\s*p\s*h\s*a\s*g\s*e\b|\bo\s*e\s*s\s*o\s*p\s*h\s*a\s*g\s*e\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]o\s*e\s*s\s*o\s*p\s*h\s*a\s*g\s*e\b|\bo\s*e\s*s\s*o\s*p\s*h\s*a\s*g\s*e\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            //Trachée
            _allStruct.Add(@"(?i)(?:t\s*[tr]\s*a\s*[ca]\s*h\s*[ae]{1,2}|\s*o\s*a\s*r\s*(?:\s*t\s*r\s*a\s*c\s*h\s*e\s*e|_t\s*r\s*a\s*c\s*h\s*e\s*e))",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:t\s*[tr]\s*a\s*[ca]\s*h\s*[ae]{1,2}|\s*o\s*a\s*r\s*(?:\s*t\s*r\s*a\s*c\s*h\s*e\s*e|_t\s*r\s*a\s*c\s*h\s*e\s*e))", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            //NOG
            _allStruct.Add(@"(?i)(?:n\s*o\s*g|n\s*e\s*r\s*f[_\s]*o\s*p\s*t\s*i\s*q\s*u\s*e[_\s]*g\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:n\s*o\s*g|n\s*e\s*r\s*f[_\s]*o\s*p\s*t\s*i\s*q\s*u\s*e[_\s]*g\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // NOD
            _allStruct.Add(@"(?i)(?:n\s*o\s*d|n\s*e\s*r\s*f[_\s]*o\s*p\s*t\s*i\s*q\s*u\s*e[_\s]*d\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:n\s*o\s*d|n\s*e\s*r\s*f[_\s]*o\s*p\s*t\s*i\s*q\s*u\s*e[_\s]*d\b)", RegexOptions.IgnoreCase))?.Id ?? "");

            // Chiasma
            _allStruct.Add(@"(?i)\b(?:c\s*h\s*i\s*a\s*s\s*m\s*a|o\s*a\s*r[_\s]*c\s*h\s*i\s*a\s*s\s*m\s*a\b)", 
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:c\s*h\s*i\s*a\s*s\s*m\s*a\b|\bo\s*a\s*r[_\s]*c\s*h\s*i\s*a\s*s\s*m\s*a\b)", RegexOptions.IgnoreCase))?.Id ?? "");

            // Encéphale
            _allStruct.Add(@"(?i)\b(?:c\s*[ce]\s*r\s*v\s*e\s*a?\s*u?\b|e\s*n\s*c\s*e\s*p\s*h\s*[ea]\s*l\s*e?\b|o\s*a\s*r[_\s]*e\s*n\s*c\s*e\s*p\s*h\s*[ea]\s*l\s*e?\b|o\s*a\s*r[_\s]*c\s*[ce]\s*r\s*v\s*e\s*a?\s*u?\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:c\s*[ce]\s*r\s*v\s*e\s*a?\s*u?\b|e\s*n\s*c\s*e\s*p\s*h\s*[ea]\s*l\s*e?\b|o\s*a\s*r[_\s]*e\s*n\s*c\s*e\s*p\s*h\s*[ea]\s*l\s*e?\b|o\s*a\s*r[_\s]*c\s*[ce]\s*r\s*v\s*e\s*a?\s*u?\b)", RegexOptions.IgnoreCase))?.Id ?? "");

            // Paroi
            _allStruct.Add(@"(?i)\b(?:p\s*a?\s*r\s*o?\s*i\s*(?:t\s*h\s*o\s*r\s*a\s*c\s*i\s*q\s*u\s*e?)?\|\bp\s*a?\s*r\s*o\s*i\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:p\s*a?\s*r\s*o?\s*i\s*(?:t\s*h\s*o\s*r\s*a\s*c\s*i\s*q\s*u\s*e?)?\|\bp\s*a?\s*r\s*o\s*i\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Poumons
            _allStruct.Add(@"(?i)\b(?:p\s*o\s*u\s*m\s*o\s*n\s*s?\b|\b*o\s*a\s*r\s*(_?\s*p\s*o\s*u\s*m\s*o\s*n\s\b))\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:\bp\s*o\s*u\s*m\s*o\s*n\s*s?\b|\s*\b*o\s*a\s*r\s*(_?\s*p\s*o\s*u\s*m\s*o\s*n\s\b))", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Poumon D
            _allStruct.Add(@"(?i)\b(?:p\s*o\s*u\s*m\s*o\s*n\s*[_\s]*d\b|\b*o\s*a\s*r\s*[_\s]*p\s*o\s*u\s*m\s*o\s*n*[_\s]*d\b)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)(?:\bp\s*o\s*u\s*m\s*o\s*n[_\s]*d\b|\s*\b*o\s*a\s*r\s*[_\s]*p\s*o\s*u\s*m\s*o\s*n*[_\s]*d\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Poumon G
            _allStruct.Add(@"(?i)\b(?:p\s*o\s*u\s*m\s*o\s*n*[_\s]*g\b|\bo\s*a\s*r*[_\s]*p\s*o\s*u\s*m\s*o\s*n*[_\s]*g\b)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:p\s*o\s*u\s*m\s*o\s*n*[_\s]*g|o\s*a\s*r*[_\s]*p\s*o\s*u\s*m\s*o\s*n*[_\s]*g)\b", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Pharynx
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r\s*[_\s]*p\s*h\s*a\s*r\s*y\s*n\s*x\b|\bp\s*h\s*a\s*r\s*y\s*n\s*x\b)\b",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r\s*[_\s]*p\s*h\s*a\s*r\s*y\s*n\s*x|\bp\s*h\s*a\s*r\s*y\s*n\s*x\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Larynx
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r\s*[_\s]*l\s*a\s*r\s*y\s*n\s*x\b|\bl\s*a\s*r\s*y\s*n\s*x\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r\s*[_\s]*l\s*a\s*r\s*y\s*n\s*x|\bl\s*a\s*r\s*y\s*n\s*x\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Thyroide
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r\s*[_\s]*t\s*h\s*y\s*r\s*o\s*i\s*d\s*e\b|\bt\s*h\s*y\s*r\s*o\s*i\s*d\s*e\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r\s*[_\s]*t\s*h\s*y\s*r\s*o\s*i\s*d\s*e\b|\bt\s*h\s*y\s*r\s*o\s*i\s*d\s*e\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Sein D 
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]*s\s*e\s*i\s*n[_\s]*d\b|\bs\s*e\s*i\s*n[_\s]*d\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]*s\s*e\s*i\s*n[_\s]*d\b|\bs\s*e\s*i\s*n[_\s]*d\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Sein G
            _allStruct.Add(@"(?i)\b(?:o\s*a\s*r[_\s]*s\s*e\s*i\s*n[_\s]*g\b|\bs\s*e\s*i\s*n[_\s]*g\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:o\s*a\s*r[_\s]*s\s*e\s*i\s*n[_\s]*g\b|\bs\s*e\s*i\s*n[_\s]*g\b)", RegexOptions.IgnoreCase))?.Id ?? "");

//OK            // Foie
            _allStruct.Add(@"(?i)\b(?:f\s*o\s*i\s*e\b|\bo\s*a\s*r\s*[_/s]*f\s*o\s*i\s*e\b)",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\b(?:f\s*o\s*i\s*e\b|\bo\s*a\s*r\s*[_\s]*f\s*o\s*i\s*e\b)", RegexOptions.IgnoreCase))?.Id ?? "");

            // Volume d'opti (cibles + ring)

            _allStruct.Add(@"(?i)\bz_gtv\s*\w*.*",
      _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bz_gtv\s*\w*.*", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bz_ctv\s*\w*.*",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bz_ctv\s*\w*.*", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bz_itv\s*\w*.*",
      _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bz_itv\s*\w*.*", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bz_ptv\s*\w*.*",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bz_ptv\s*\w*.*", RegexOptions.IgnoreCase))?.Id ?? "");
            _allStruct.Add(@"(?i)\bz_ring\s*\w*.*",
                _ss.Structures.FirstOrDefault(s => Regex.IsMatch(s.Id, @"(?i)\bz_ring\s*\w*.*", RegexOptions.IgnoreCase))?.Id ?? "");
        }
        #endregion
    }
}