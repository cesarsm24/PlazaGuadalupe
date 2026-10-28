using System.Collections.Generic;
using UnityEditor;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Normas de importación de las animaciones (<c>Art/Animations</c>): Humanoid, sin malla ni materiales y con el
    /// movimiento de la raíz integrado en la pose. Todas en bucle salvo las de <see cref="OneShots"/>.
    /// </summary>
    /// <remarks>Como <see cref="ModelImportRules"/>, se aplican solo en la primera importación.</remarks>
    internal sealed class AnimationImportRules : AssetPostprocessor
    {
        private const string AnimationsFolder = ProjectPaths.Animations + "/";

        /// <summary>Animaciones que se reproducen una sola vez: transiciones y gestos.</summary>
        internal static readonly HashSet<string> OneShots = new HashSet<string>
        {
            "StandToSit", "SitToStand", "Pointing", "PointingForward",
            "Waving", "Beckoning", "HeadNodYes", "HardHeadNod", "Drinking", "SittingDrinking",
            "Laughing", "SittingLaughing", "QuickFormalBow", "ShakingHands",
        };

        /// <summary>Transiciones entre estar de pie y sentado; el turista se desliza hasta el asiento por su cuenta.</summary>
        internal static readonly HashSet<string> SeatTransitions = new HashSet<string> { "StandToSit", "SitToStand" };

        /// <summary>
        /// Animaciones en las que el cuerpo está lejos del origen o se desplaza aunque el personaje no deba moverse
        /// (beber sentado acaba casi a 3 m de donde empieza la toma; mirar alrededor avanza unos 0,8 m).
        /// </summary>
        internal static readonly HashSet<string> Drifting = new HashSet<string> { "SittingDrinking", "LookingAround", "ShakingHands" };

        /// <summary>
        /// Animaciones que empiezan con un tramo que no sirve, y segundo desde el que se usan. Beber sentado empieza de
        /// pie: anda, se da la vuelta y se sienta; quien ya está sentado solo debe hacer lo de después.
        /// </summary>
        internal static readonly Dictionary<string, float> StartsAt = new Dictionary<string, float> { ["SittingDrinking"] = 5f };

        private const float FrameRate = 30f; // fps de los clips

        /// <summary>Si el desplazamiento horizontal se queda en la pose (true) o se descarta como movimiento de la raíz.</summary>
        internal static bool BakesPositionXZ(string clip) => !SeatTransitions.Contains(clip) && !Drifting.Contains(clip);

        /// <summary>
        /// Aplica a un clip lo de <see cref="StartsAt"/>: lo recorta y lo coloca y orienta según el cuerpo en su primer
        /// fotograma, porque ahí ya no está donde estaba al empezar la toma ni mira hacia donde miraba.
        /// </summary>
        internal static void Trim(ModelImporterClipAnimation clip, string name)
        {
            if (StartsAt.TryGetValue(name, out float seconds))
            {
                clip.firstFrame = UnityEngine.Mathf.Round(seconds * FrameRate);
                clip.keepOriginalOrientation = false;
                clip.keepOriginalPositionXZ = false;
            }
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(AnimationsFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(AnimationsFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool loop = !OneShots.Contains(name);
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = name;
                clip.loopTime = loop;
                // Es la NavMesh la que desplaza y gira a los personajes.
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = true;
                // En las transiciones y en las que se desplazan solas el avance queda como movimiento de la raíz,
                // que se descarta; integrado en la pose, el cuerpo avanzaría dos veces o se iría de su sitio.
                clip.lockRootPositionXZ = BakesPositionXZ(name);
                clip.keepOriginalPositionXZ = true;
                Trim(clip, name);
            }

            importer.clipAnimations = clips;
        }
    }
}
