/******************************************************************************
 * Nom du fichier : CreateVolume.cs
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


using Opti_Struct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

//***************************************************************
//
// Cette classe est la classe d'application principale
// Elle permet les opérations sur les structures 
// Elle s'assure également de la pertinence des structures engagées dans les opérations
// Elle alimente le fichier log qui permet un décriptage des actions effectuées

namespace Structure_optimisation
{
    internal class CreateVolume
    {
        private string name;
        private readonly char[] filterTags;
        private StructureSet _ss;
        private Course _course;
        private Image _image;
        private StreamReader srf;
        private readonly Dictionary<char, Action<Structure, Structure>> _structure;
        private string line;
        private string _message;
        public event EventHandler<string> MessageChanged;
        private int verbose;
        private Struct_dictionnary _dictionnary;
        private UserInterfaceModel _model;

        #region Constructeurs
        #region Constructeur 1
        public CreateVolume(StructureSet ss, Course course, Image image)
        {
            name = string.Empty;
            _ss = ss;
            _course = course;
            _image = image;
            verbose = 1;
            filterTags = new char[] {
                '|', // totalite
                '&', // intersection
                '#', // soustraction  
                '^', // union
                '!', // Marge
                '(', // parenthese ouvre
                ')', // parenthese fermee
                '/', // Suppression structure
                ',' // Marges asymétriques
				//'!' // inversion
				};
            _dictionnary = new Struct_dictionnary(_ss);
            _structure = new Dictionary<char, Action<Structure, Structure>>()
{
    { filterTags[0], (arg1, arg2) => arg1.SegmentVolume = arg1.Or(arg2)},  // Or est la totalité
	{ filterTags[1], (arg1, arg2) => arg1.SegmentVolume = arg1.And(arg2)}, // And est  l'intersection
	{ filterTags[2], (arg1, arg2) => arg1.SegmentVolume = arg1.Sub(arg2)}, // sub est la soustraction et conserve que Arg1
	{ filterTags[3], (arg1, arg2) => arg1.SegmentVolume = arg1.Xor(arg2)}, // Xor est l'union
};
            _dictionnary.MessageChanged += DictionnaryMessageChanged;
        }
        #endregion
        #region Constructeur 2
        public CreateVolume(UserInterfaceModel model)
        {
            name = string.Empty;
            _ss = model.GetContext.StructureSet;
            _course = model.GetContext.Course;
            _image = model.GetContext.Image;
            _model = model;
            verbose = 1;
            filterTags = new char[] {
                '|', // totalite
                '&', // intersection
                '#', // soustraction  
                '^', // union
                '!', // Marge
                '(', // parenthese ouvre
                ')', // parenthese fermee
                '/', // Suppression structure
                ',' // Marges asymétriques
				//'!' // inversion
				};
            _dictionnary = new Struct_dictionnary(_ss);
            _structure = new Dictionary<char, Action<Structure, Structure>>()
{
    { filterTags[0], (arg1, arg2) => arg1.SegmentVolume = arg1.Or(arg2)},  // Or est la totalité
	{ filterTags[1], (arg1, arg2) => arg1.SegmentVolume = arg1.And(arg2)}, // And est  l'intersection
	{ filterTags[2], (arg1, arg2) => arg1.SegmentVolume = arg1.Sub(arg2)}, // sub est la soustraction et conserve que Arg1
	{ filterTags[3], (arg1, arg2) => arg1.SegmentVolume = arg1.Xor(arg2)}, // Xor est l'union
};
            _dictionnary.MessageChanged += DictionnaryMessageChanged;
        }
        #endregion
        #endregion

        private void _dictionnary_MessageChanged(object sender, string e)
        {
            throw new NotImplementedException();
        }

        internal void CreationVolume(string _userFileChoice, List<string> targets)
        {
            string erreur = string.Empty, information = string.Empty;


            #region Suppression des anciennes structures d'opti
            // Suppresion des anciennes traces dans eclipse de l'exécution en auto :
            // _auto dans le nom du SS et auto dans le nom des Structures créées
            // On commence par supprimer toutes les structures auto pour pouvoir les recréer
            Message = $"\nSuppression des anciennes structures auto ...";
            try
            {
                if (!_ss.Id.Contains("auto"))
                {
                    _ss.Id = "Dosi_auto";
                    Message = $"StructureSet renommé en {_ss.Id}";
                }
            }
            catch { MessageBox.Show("La dénomination auto n'a pas été appliqué au StructureSet car le nombre de caratère dépasse 16"); }
            try
            {
                _ss.Structures.Where(s => s.Id.Contains("auto")).ToList().ForEach(s => _ss.RemoveStructure(s));
                Message = $"Suppression réalisée avec succés";
            }
            catch (Exception ex)
            {
                Message = $"Suppression en échec";
                Message = ex.Message;
            }
            Message = "\n";
            #endregion

            #region Body creation

            Structure BODY = _ss.Structures.Where(x => x.DicomType.ToUpper().Equals("EXTERNAL")).SingleOrDefault();
            try
            {
                SearchBodyParameters _bodyParameters = _ss.GetDefaultSearchBodyParameters();
                _bodyParameters.KeepLargestParts = true;
                _bodyParameters.FillAllCavities = true;
                _bodyParameters.Smoothing = true;
                _bodyParameters.SmoothingLevel = 2;

                if (_userFileChoice.ToLower().Contains("sein"))
                {
                    _ss.RemoveStructure(BODY);
                    _bodyParameters.LowerHUThreshold = -700;
                    BODY = _ss.CreateAndSearchBody(_bodyParameters);
                    Message = $"Modification du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n";
                    information += $"✅ Modification du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n\n";
                }
                else if (_userFileChoice.ToLower().Contains("orl"))
                {
                    _ss.RemoveStructure(BODY);
                    _bodyParameters.LowerHUThreshold = -350;
                    BODY = _ss.CreateAndSearchBody(_bodyParameters);
                    Message = $"Modification du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n";
                    information += $"✅ Modification du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n\n";
                }
                else if (!_ss.Structures.Any(x => x.DicomType.ToLower().Equals("body")))
                {
                    _bodyParameters.LowerHUThreshold = -350;
                    BODY = _ss.CreateAndSearchBody(_bodyParameters);
                    Message = $"Création du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n";
                    information += $"✅ Création du contour externe ; seuil défini à : {_bodyParameters.LowerHUThreshold.ToString()} UH\n\n";
                }
                else
                {
                    Message = $"Aucune modification du contour externe\n";
                    information += $"✅ Aucune modification du contour externe\n\n";
                }

                Message = $"Modification de la couleur du contour externe en : vert (code RGB (0,255,0))\n";
                BODY.Color = Color.FromRgb(0, 255, 0);
                BODY.Id = "Contour externe";
                BODY.Comment = "Structure générée automatiquement";
            }
            catch
            {
                erreur += $"❌ la structure est peut être déjà approuvé un il existe une erreur de nom ou de couleur ou de commentaire " +
                    $"sur la structure Contour externe\n\n";
            }
            #endregion

            #region Assignation des targets
            string fileContent = File.ReadAllText(System.IO.Path.GetFullPath(_userFileChoice));
            string fileContent_original = fileContent;
            switch (targets.Count())
            {
                case 1:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    break;
                case 2:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    fileContent = fileContent.Replace("cible2", targets[1]);
                    break;
                case 3:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    fileContent = fileContent.Replace("cible2", targets[1]);
                    fileContent = fileContent.Replace("cible3", targets[2]);
                    break;
                case 4:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    fileContent = fileContent.Replace("cible2", targets[1]);
                    fileContent = fileContent.Replace("cible3", targets[2]);
                    fileContent = fileContent.Replace("cible4", targets[3]);
                    break;
                case 5:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    fileContent = fileContent.Replace("cible2", targets[1]);
                    fileContent = fileContent.Replace("cible3", targets[2]);
                    fileContent = fileContent.Replace("cible4", targets[3]);
                    fileContent = fileContent.Replace("cible5", targets[4]);
                    break;
                case 6:
                    fileContent = fileContent.Replace("cible1", targets[0]);
                    fileContent = fileContent.Replace("cible2", targets[1]);
                    fileContent = fileContent.Replace("cible3", targets[2]);
                    fileContent = fileContent.Replace("cible4", targets[3]);
                    fileContent = fileContent.Replace("cible5", targets[4]);
                    fileContent = fileContent.Replace("cible6", targets[5]);
                    break;

            }
            File.WriteAllText(System.IO.Path.GetFullPath(_userFileChoice), fileContent);
            #endregion

            srf = new StreamReader(System.IO.Path.GetFullPath(_userFileChoice));
            List<string> nSplit = new List<string>();
            List<char> indexeur = new List<char>();
            line = "Start";
            Random col = new Random();

            #region Read file
            while ((line = srf.ReadLine()) != null)
            {
                name = line.Split(':')[0].Trim();
                line = line.Split(':')[1].Trim();
                byte color1 = (byte)col.Next(100, 200);
                byte color2 = (byte)col.Next(100, 200);
                nSplit.Clear();
                indexeur.Clear();
                Structure myStruct, _StructureInter, _StructureInter1, _StructureInter2;
                Message = $"Travail sur la structure {name}";
                bool alreadyDone = false;

                try
                {
                    #region Verbose
                    if (name.ToLower().Contains("verbose"))
                    {
                        verbose = int.Parse(line);
                        Message = $"Niveau de verbose {verbose.ToString()}\n";
                        continue;
                    }
                    #endregion

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    #region Table
                    if (name.ToLower().Equals("table") && !_ss.Structures.Any(x => x.Id.Trim().ToLower() == "couchinterior" || x.Id.Trim().ToLower() == "couchsurface"))
                    {
                        Message = $"Travail sur la structure {name} en cours ....\n";
                        try
                        {
                            IReadOnlyList<Structure> couchStructureList = _ss.Structures.ToList();
                            bool ImageResized = false;
                            string error = "Erreur dans la création de la table";
                            double interiorHu = -1000, surfaceHu = -300;

                            if (_course.PlanSetups.Any(x => x.Beams.Any(y => y.TreatmentUnit.Id.ToLower().Contains("halcyon"))))
                            {
                                _ss.AddCouchStructures("RDS_Couch_Top", PatientOrientation.NoOrientation, RailPosition.In, RailPosition.In, null, null, null, out couchStructureList, out ImageResized, out error);
                                Message = $"Ajout de la structure Table Halcyon";
                                _ss.Structures.Where(x => x.DicomType.ToLower().Equals("support")).ToList().ForEach(structure => structure.Comment = "Table Halcyon générée automatiquement");
                                name = "Table Halcyon";
                            }
                            else if (line.ToLower().Equals("fine"))
                            {
                                _ss.AddCouchStructures("Exact_IGRT_Couch_Top_thin", PatientOrientation.NoOrientation, RailPosition.In, RailPosition.In, surfaceHu, interiorHu, null, out couchStructureList, out ImageResized, out error);
                                Message = $"Ajout de la structure Table Truebeam fine";
                                _ss.Structures.Where(x => x.DicomType.ToLower().Equals("support")).ToList().ForEach(structure => structure.Comment = "Table Truebeam fine générée automatiquement");
                                name = "Table Truebeam fine";
                            }
                            else if (line.ToLower().Equals("moyenne"))
                            {
                                _ss.AddCouchStructures("Exact_IGRT_Couch_Top_medium", PatientOrientation.NoOrientation, RailPosition.In, RailPosition.In, surfaceHu, interiorHu, null, out couchStructureList, out ImageResized, out error);
                                Message = $"Ajout de la structure Table Truebeam medium";
                                _ss.Structures.Where(x => x.DicomType.ToLower().Equals("support")).ToList().ForEach(structure => structure.Comment = "Table Truebeam médium générée automatiquement");
                                name = "Table Truebeam moyenne";
                            }
                            else if (line.ToLower().Equals("epaisse"))
                            {
                                _ss.AddCouchStructures("Exact_IGRT_Couch_Top_thick", PatientOrientation.NoOrientation, RailPosition.In, RailPosition.In, surfaceHu, interiorHu, null, out couchStructureList, out ImageResized, out error);
                                Message = $"Ajout de la structure Table Truebeam epaisse";
                                _ss.Structures.Where(x => x.DicomType.ToLower().Equals("support")).ToList().ForEach(structure => structure.Comment = "Table Truebeam épaisse générée automatiquement");
                                name = "Table Truebeam épaisse";
                            }

                            VVector Isocenter = _course.PlanSetups.SelectMany(ps => ps.Beams).Where(b => b.IsocenterPosition != null).Select(b => b.IsocenterPosition).FirstOrDefault();
                            VVector Couchposition = new VVector();
                            Couchposition.x = _image.Origin.x - Isocenter.x + 0;
                            Couchposition.y = _image.Origin.y - Isocenter.y + 0;
                            Couchposition.z = _image.Origin.z - Isocenter.z + 100; // DSA = 100 cm

                            foreach (var item in _ss.Structures)
                            {
                                if (item.Id.ToLower().Contains("couch"))
                                {
                                    item.CenterPoint.Update(VVector.Component.X, Couchposition.x);
                                    item.CenterPoint.Update(VVector.Component.Y, Couchposition.y);
                                    item.CenterPoint.Update(VVector.Component.Z, Couchposition.z);
                                }
                            }
                            Message = $"Fin des opération sur la structure de table\n";
                            Message = $"********** Ajout de la table de traitement **********";
                            Message = $"************ Positionnement non vérifié *************";
                            Message = $"*****************************************************";
                            information += $"✅ Structure de table {name} créée \n\n";
                        }
                        catch (Exception ex)
                        {
                            Message = $"Erreur dans la manipulation de la table de traitement\n {ex.Message}";
                            Message = $"Erreur dans la création de la table de traitement";
                            Message = $"indice d'erreur : 1";
                            erreur += $"❌ Erreur sur la structure : table\n\n";
                        }
                    }
                    #endregion

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    #region Operation
                    foreach (char key in filterTags)
                    {

                        if (line.Contains(key) && !alreadyDone)
                        {
                            #region Margin
                            if (key == filterTags[4]) // cas x+marge
                            {
                                string[] V = line.Split(filterTags[4]);
                                try
                                {
                                    Message = $"********** Operation simple **********";
                                    Message = $"Structure attendue : {V[0]}";

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Trim().Equals(V[0].ToLower().Trim(), StringComparison.OrdinalIgnoreCase))) // modif
                                    {
                                        Message = $"Mauvaise nomenclature sur la structure {V[0]}";
                                        V[0] = _dictionnary.SearchStruct(V[0]);
                                    }

                                    Structure Struct1 = _ss.Structures.FirstOrDefault(x => x.Id.ToLower().Trim().Equals(V[0].Trim().ToLower(), StringComparison.OrdinalIgnoreCase))
                        ?? _ss.Structures.FirstOrDefault(x => Regex.IsMatch(x.Id, string.Join(@".*", V[0].Trim().Select(c => Regex.Escape(c.ToString() + @"\s*"))), RegexOptions.IgnoreCase));

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Equals(name.ToLower())))
                                        if (Regex.IsMatch(name, @"\b(?:.*(?:ITV|PTV|GTV|CTV).*\b)", RegexOptions.IgnoreCase))
                                        {
                                            myStruct = _ss.AddStructure("PTV", name);
                                            myStruct.Color = Color.FromRgb(255, 0, 0);
                                        }
                                        else if (name.ToLower().Contains("externe +7") || name.ToLower().Contains("externe -7"))
                                        {
                                            myStruct = _ss.AddStructure("Control", name);
                                            myStruct.Color = Color.FromRgb(255, 0, 255);
                                        }
                                        else
                                        {
                                            myStruct = _ss.AddStructure("Control", name);
                                            myStruct.Color = Color.FromRgb(color1, color2, 255);
                                        }
                                    else if (name.ToLower() == Struct1.Id.ToLower())
                                        myStruct = Struct1;
                                    else
                                        myStruct = _ss.Structures.Where(x => x.Id.ToLower().Equals(name.ToLower())).SingleOrDefault();


                                    // Asymetric margin
                                    if (line.Contains(filterTags[8]))
                                    {
                                        float[] V_f = V[1].Split(filterTags[8]).Select(float.Parse).ToArray();

                                        try
                                        {
                                            AxisAlignedMargins _margin;
                                            if (V_f.Any(x => x < 0))
                                            {
                                                // dans l'ordre : x1, x2, y1, y2, z1 et z2
                                                // ordre dans la fonction :  droite, avant, bas, gauche, arrière, haut
                                                // ici droite puis gauche, arrière puis avant, bas puis haut
                                                _margin = new AxisAlignedMargins(StructureMarginGeometry.Inner, Math.Abs(V_f[0]), Math.Abs(V_f[3]), Math.Abs(V_f[4]), Math.Abs(V_f[1]), Math.Abs(V_f[2]), Math.Abs(V_f[5]));
                                            }
                                            else
                                            {
                                                // dans l'ordre : x1, x2, y1, y2, z1 et z2
                                                // ordre dans la fonction :  droite, avant, bas, gauche, arrière, haut
                                                // ici droite puis gauche, arrière puis avant, bas puis haut
                                                _margin = new AxisAlignedMargins(StructureMarginGeometry.Outer, V_f[0], V_f[3], V_f[4], V_f[1], V_f[2], V_f[5]);
                                            }
                                            myStruct.SegmentVolume = Struct1.SegmentVolume.AsymmetricMargin(_margin);
                                            Message = $"Marge asymétriques de {V_f[0]} mm à droite,\n{V_f[3]} mm à gauche,\n{V_f[4]} mm en arrière,\n{V_f[1]} mm en avant,\n{V_f[2]} mm en bas,\n{V_f[5]} mm haut sur la structure : {Struct1.Id}";
                                            Message = $"Structure {name} créée";
                                            information += $"✅ {name} : Marge asymétriques de {V_f[0]} mm à droite,\n{V_f[3]} mm à gauche,\n{V_f[4]} mm en arrière,\n{V_f[1]} mm en avant,\n{V_f[2]} mm en bas,\n{V_f[5]} mm haut sur la structure : {Struct1.Id}\n\n"; ;
                                        }
                                        catch
                                        {
                                            erreur += $"❌ Erreur sur la structure : {name} lors de l'opération de marges asymétriques de : {V_f[0]} mm à droite,{V_f[3]} mm à gauche,{V_f[4]} mm en arrière," +
                                                $"{V_f[1]} mm en avant,{V_f[2]} mm en bas,{V_f[5]} en haut sur {V[0]}\n\n";
                                            string[] ErrorStruct = { myStruct.DicomType, myStruct.Id };
                                            _ss.RemoveStructure(myStruct);
                                            _ss.AddStructure(ErrorStruct[0], ErrorStruct[1]);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            myStruct.SegmentVolume = Struct1.Margin(float.Parse(V[1]));
                                            Message = $"Marge symétriques de {V[1]} mm sur la structure : {Struct1.Id}";
                                            Message = $"Structure {name} créée";
                                            information += $"✅ {name} : Marge symétriques de {V[1]} mm sur la structure : {Struct1.Id}\n\n";
                                        }
                                        catch
                                        {
                                            erreur += $"❌ Erreur sur la structure {name} lors de l'opération de marges de {V[1]} mm symétriques sur {V[0]}\n\n";
                                            string[] ErrorStruct = { myStruct.DicomType, myStruct.Id };
                                            _ss.RemoveStructure(myStruct);
                                            _ss.AddStructure(ErrorStruct[0], ErrorStruct[1]);
                                            continue;
                                        }
                                    }
                                    if (!name.ToLower().Contains("externe"))
                                        myStruct.SegmentVolume = myStruct.And(BODY);
                                    myStruct.Comment = "Structure générée automatiquement";
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    Message = $"{ex.Message}";
                                    Message = $"La structure de l'utilisateur est ajoutée vide";
                                    Message = $"Indice erreur : 2";
                                }
                            }
                            #endregion

                            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            #region All others operations
                            else if (key != filterTags[4] && key != filterTags[5] && key != filterTags[6] && key != filterTags[7] && key != filterTags[8]) // toutes les autres opérations
                            {

                                #region multiple structures

                                Message = $"********** Operation multiple **********";

                                string[] _parts = line.Split(filterTags, StringSplitOptions.RemoveEmptyEntries);

                                //if (filterTags.Any(c => line.Count(ch => ch == c) > 1)) // plusieurs structures
                                if (filterTags.Sum(tag => line.Count(ch => ch == tag)) > 1)
                                {
                                    foreach (string part in _parts)
                                    {
                                        string part_corr = part; // modif

                                        Message = $"Structure attendue : {part}";

                                        if (!_ss.Structures.Any(x => x.Id.ToLower().Trim().Equals(part.ToLower().Trim())))  // modif
                                        {
                                            Message = $"Mauvaise nomenclature sur la structure {part}";
                                            part_corr = _dictionnary.SearchStruct(part); // modif
                                            if (part_corr != part)
                                                Message = $"Modification du nom de la structure {part} par {part_corr}";
                                        }
                                        nSplit.Add(part_corr.Trim()); // modif
                                    }

                                    foreach (char c in line)
                                    {
                                        if (Array.IndexOf(filterTags, c) != -1)
                                        {
                                            indexeur.Add(c);
                                        }
                                    }

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Equals(name.ToLower())))
                                        if (Regex.IsMatch(name, @"\b(?:.*(?:ITV|PTV|GTV|CTV).*\b)", RegexOptions.IgnoreCase))
                                        {
                                            _StructureInter = _ss.AddStructure("PTV", name);
                                            _StructureInter.Color = Color.FromRgb(255, 0, 0);
                                        }
                                        else
                                        {
                                            _StructureInter = _ss.AddStructure("Control", name);
                                            _StructureInter.Color = Color.FromRgb(color1, color2, 255);
                                        }
                                    else
                                        _StructureInter = _ss.Structures.Where(x => x.Id.ToLower().Equals(name.ToLower())).SingleOrDefault();

                                    try
                                    {
                                        if (name == nSplit[0])
                                            _StructureInter1 = _StructureInter;
                                        else
                                            _StructureInter1 = _ss.Structures.Where(x => x.Id.ToLower().Equals(nSplit[0].Trim().ToLower())).SingleOrDefault();

                                        _StructureInter2 = _ss.Structures.Where(x => x.Id.ToLower().Equals(nSplit[1].Trim().ToLower())).SingleOrDefault();

                                        string operation = (nSplit[0] + " " + indexeur[0] + " " + nSplit[1]);

                                        foreach (var key2 in _structure.Keys)
                                        {
                                            if (operation.Contains(key2))
                                            {
                                                _StructureInter.SegmentVolume = _StructureInter1.Margin(0.00);
                                                _structure[key2](_StructureInter, _StructureInter2);
                                            }
                                        }

                                        Message = $"Operation sur les structures : {_StructureInter.Id} et {_StructureInter2.Id}";
                                        Message = $"Structure {name} créée";

                                        nSplit[0] = name.ToLower();

                                        for (int i = 1; i < nSplit.Count - 1; i++)
                                        {
                                            operation = nSplit[0] + " " + indexeur[i] + " " + nSplit[i + 1];

                                            // foreach (var key2 in _structure.Keys)
                                            //{
                                            //if (operation.Contains(key2))
                                            //{
                                            _StructureInter1 = _ss.Structures.Where(x => x.Id.ToLower().Equals(nSplit[i + 1].ToLower())).SingleOrDefault();
                                            _structure[indexeur[i]](_StructureInter, _StructureInter1);
                                            // }
                                            // }
                                        }
                                        _StructureInter.SegmentVolume = _StructureInter.And(BODY);
                                        _StructureInter.Comment = "Structure générée automatiquement";

                                        Message = $"Operation sur les structures : {_StructureInter.Id} et {_StructureInter1.Id}";
                                        Message = $"Structure {name} créée";
                                        information += $"✅ {_StructureInter.Id} : Operation sur les structures : {_StructureInter1.Id} et {_StructureInter2.Id}\n\n";
                                        alreadyDone = true;
                                        continue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Message = $"{ex.Message}";
                                        Message = $"La structure de l'utilisateur est ajoutée vide";
                                        Message = $"Indice erreur : 3";

                                        erreur += $"❌ Erreur sur la structure {name} lors l'opération multiple {line}\n\n";
                                        string[] ErrorStruct = { _StructureInter.DicomType, _StructureInter.Id };
                                        _ss.RemoveStructure(_StructureInter);
                                        _ss.AddStructure(ErrorStruct[0], ErrorStruct[1]);
                                        alreadyDone = true;
                                        continue;
                                    }

                                }
                                #endregion

                                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                                #region 2 structures
                                else // Opération sur 2 structures
                                {
                                    string[] V = line.Split(key);
                                    Message = $"********** Operation simple **********";
                                    Message = $"Operation simple {key}";
                                    Message = $"Structures attendues : {V[0]} et {V[1]}";

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Trim().Equals(V[0].ToLower().Trim())))  // modif
                                    {
                                        Message = $"Mauvaise nomenclature sur la structure {V[0]}";
                                        V[0] = _dictionnary.SearchStruct(V[0]);
                                    }

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Trim().Equals(V[1].ToLower().Trim())))  // modif
                                    {
                                        Message = $"Mauvaise nomenclature sur la structure {V[1]}";
                                        V[1] = _dictionnary.SearchStruct(V[1]);
                                    }

                                    Structure Struct1 = _ss.Structures.Where(x => x.Id.ToLower().Equals(V[0].Trim().ToLower())).SingleOrDefault();
                                    Structure Struct2 = _ss.Structures.Where(x => x.Id.ToLower().Equals(V[1].Trim().ToLower())).SingleOrDefault();

                                    if (!_ss.Structures.Any(x => x.Id.ToLower().Equals(name.ToLower())))
                                        if (Regex.IsMatch(name, @"\b(?:.*(?:ITV|PTV|GTV|CTV).*\b)", RegexOptions.IgnoreCase))
                                        {
                                            myStruct = _ss.AddStructure("PTV", name);
                                            myStruct.Color = Color.FromRgb(255, 0, 0);
                                        }
                                        else
                                        {
                                            myStruct = _ss.AddStructure("Control", name);
                                            myStruct.Color = Color.FromRgb(color1, color2, 255);
                                        }
                                    else if (name.ToLower() == Struct1.Id.ToLower())
                                        myStruct = Struct1;
                                    else
                                        myStruct = _ss.Structures.Where(x => x.Id.ToLower().Equals(name.ToLower())).SingleOrDefault();

                                    try
                                    {
                                        myStruct.SegmentVolume = Struct1.Margin(0.00);
                                        _structure[key](myStruct, Struct2);
                                        myStruct.SegmentVolume = myStruct.And(BODY);
                                        myStruct.Comment = "Structure générée automatiquement";

                                        Message = $"Operation sur les structures : {Struct1.Id} et {Struct2.Id}";
                                        Message = $"Structure {name} créée";
                                        information += $"✅ {name} : Operation sur les structures : {Struct1.Id} et {Struct2.Id}\n\n";
                                        continue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Message = $"{ex.Message}";
                                        Message = $"La structure de l'utilisateur est ajoutée vide";
                                        Message = $"Indice erreur : 4";

                                        erreur += $"❌ Erreur sur la structure {name} lors l'opération {key} sur  {V[0]} et {V[1]}\n\n";
                                        string[] ErrorStruct = { myStruct.DicomType, myStruct.Id };
                                        _ss.RemoveStructure(myStruct);
                                        _ss.AddStructure(ErrorStruct[0], ErrorStruct[1]);
                                        continue;
                                    }
                                }
                                #endregion
                            }
                            #endregion

                            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            #region Remove structure
                            else if (key == filterTags[7])
                            {
                                try
                                {
                                    Message = $"********** Operation simple **********";
                                    Message = $"Operation simple {key}";
                                    _ss.RemoveStructure(_ss.Structures.FirstOrDefault(x => Regex.IsMatch(x.Id, @"\bt\s*e\s*s\s*t*[_\s]*(?:[1-9]|10)?.*?(?!\bintestin\b)", RegexOptions.IgnoreCase)));
                                    Message = $"Suppression de structure";
                                    Message = $"Structure {name} supprimée";
                                    information += $"✅ {name} : Structure supprimée\n\n";
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    Message = $"{ex.Message}";
                                    Message = $"La structure de l'utilisateur est non modifiée";
                                    Message = $"Indice erreur : 5";
                                    erreur += $"❌ Erreur sur la structure {name} lors l'opération de suppression\n\n";
                                    continue;
                                }
                            }
                        }
                        #endregion


                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        #region Create empty structure
                        else if (line.Length == 0 && !line.Contains(key)) // Créé une structure vide
                        {
                            try
                            {
                                Message = $"********** Operation simple **********";
                                Message = $"Operation simple : création de structure vide";

                                if (Regex.IsMatch(name, @"\b(?:.*(?:ITV|PTV|GTV|CTV).*\b)", RegexOptions.IgnoreCase))
                                {
                                    myStruct = _ss.AddStructure("PTV", name);
                                    myStruct.Color = Color.FromRgb(255, 0, 0);
                                }
                                else
                                {
                                    myStruct = _ss.AddStructure("Control", name);
                                    myStruct.Color = Color.FromRgb(color1, color2, 255);
                                }

                                myStruct.Comment = "Structure générée automatiquement";
                                Message = $"Création de structure";
                                Message = $"Structure {name} créée";
                                information += $"✅ {name} : Structure créée vide\n\n";
                                break;
                            }
                            catch (Exception ex)
                            {
                                Message = $"{ex.Message}";
                                Message = $"La structure de l'utilisateur n'a pas pu être ajoutée";
                                Message = $"Indice erreur : 6";
                                erreur += $"❌ Erreur sur la structure {name} lors l'opération de création de structure vide\n\n";
                                break;
                            }
                        }
                        #endregion

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    }
                }
                #endregion
                catch (Exception ex)
                {
                    if (verbose >= 1)
                        MessageBox.Show($"Une erreur est survenue sur la structure {name} : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    Message = $"Une erreur est survenue sur la structure {name} : " + ex.Message;
                }
                Message = "\n**********************************************************************************************************\n";
            }

            foreach (var autostruct in _ss.Structures)
            {
                if (autostruct.Comment.Contains("automatiquement"))
                    autostruct.Id = autostruct.Id + "_auto";
            }

            MessageRecap(information, erreur, System.IO.Path.GetFileNameWithoutExtension(_userFileChoice));
            _model.Set_Log = erreur;
            #endregion

            srf.Close();
            try
            {
                Message = $"\nSuccés dans la régénération du fichier de départ\n";
                Message = $"Détail du fichier de départ :\n\n{fileContent_original}\n\nDétail du fichier de fin :\n\n{fileContent}\n";
                File.WriteAllText(System.IO.Path.GetFullPath(_userFileChoice), fileContent_original);
            }
            catch (Exception ex)
            {
                Message = $"\nErreur :\n {ex.Message}\n";
                Message = $"\nErreur dans la régénération du fichier de départ\n";
                Message = $"Fichier actuel non approuvé :\n\n{fileContent}\n";
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #region Message
        internal void MessageRecap(string information, string erreur, string file)
        {
            VolumeRecap recap = new VolumeRecap(information, erreur, file);
            recap.ShowDialog();
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
        private void DictionnaryMessageChanged(object sender, string e)
        {
            Message = e;
        }
        protected virtual void OnMessageChanged()
        {
            MessageChanged?.Invoke(this, _message);
        }
        #endregion
    }
}
