using UnityEngine;

namespace SoftballConnect.Presentation
{
    // Original adult-proportioned placeholders, articulated independently of the simulation.
    public sealed class CharacterView : MonoBehaviour
    {
        Transform modelRoot, arm, glove, leftShoe, rightShoe, leftHand, rightHand;
        Vector3 leftRest, rightRest;
        float idleTime;

        public void Initialize(ArcadeArt art, Color team, bool helmet = false)
        {
            modelRoot = art.Group("ModelRoot", transform, Vector3.zero);
            InitializeAthlete(art, team, helmet);
        }

        void InitializeAthlete(ArcadeArt art, Color team, bool helmet)
        {
            // Adult-sized placeholder with a 1.75 m stance; no licensed character model.
            var uniform = new Color(0.86f, 0.86f, 0.82f);
            art.Shape("Jersey", PrimitiveType.Capsule, modelRoot, new Vector3(0, 1.17f, 0), new Vector3(0.43f, 0.29f, 0.27f), uniform);
            art.Box("Belt", modelRoot, new Vector3(0, 0.94f, 0), new Vector3(0.35f, 0.045f, 0.24f), team);
            foreach (float side in new[] { -1f, 1f })
            {
                art.Shape("TrouserLeg", PrimitiveType.Capsule, modelRoot, new Vector3(side * 0.12f, 0.55f, 0), new Vector3(0.20f, 0.38f, 0.22f), uniform);
                art.Shape("Sock", PrimitiveType.Capsule, modelRoot, new Vector3(side * 0.12f, 0.22f, 0), new Vector3(0.13f, 0.16f, 0.14f), team);
            }
            leftShoe = art.Orb("LeftCleat", modelRoot, new Vector3(-0.12f, 0.07f, 0.055f), new Vector3(0.15f, 0.13f, 0.30f), ArcadeArt.Ink);
            rightShoe = art.Orb("RightCleat", modelRoot, new Vector3(0.12f, 0.07f, 0.055f), new Vector3(0.15f, 0.13f, 0.30f), ArcadeArt.Ink);
            art.Orb("Neck", modelRoot, new Vector3(0, 1.47f, 0), new Vector3(0.13f, 0.15f, 0.13f), ArcadeArt.Skin);
            art.Orb("Head", modelRoot, new Vector3(0, 1.63f, 0), new Vector3(0.23f, 0.29f, 0.24f), ArcadeArt.Skin);
            art.Orb(helmet ? "Helmet" : "Cap", modelRoot, new Vector3(0, 1.75f, -0.015f), new Vector3(0.27f, 0.13f, 0.27f), team);
            art.Box("CapBrim", modelRoot, new Vector3(0, 1.73f, 0.15f), new Vector3(0.24f, 0.02f, 0.15f), team);
            foreach (float side in new[] { -1f, 1f })
                art.Orb("Eye", modelRoot, new Vector3(side * 0.045f, 1.65f, 0.114f), new Vector3(0.018f, 0.018f, 0.012f), ArcadeArt.Ink);
            arm = art.Group("ThrowArm", modelRoot, new Vector3(0.25f, 1.38f, 0));
            art.Segment("ThrowSleeve", arm, Vector3.zero, new Vector3(0.02f, -0.23f, 0), 0.07f, team);
            art.Segment("ThrowForearm", arm, new Vector3(0.02f, -0.23f, 0), new Vector3(0, -0.52f, 0), 0.045f, ArcadeArt.Skin);
            rightHand = art.Orb("ThrowHand", arm, new Vector3(0, -0.60f, 0.03f), new Vector3(0.10f, 0.15f, 0.07f), ArcadeArt.Skin);
            art.Segment("LeftSleeve", modelRoot, new Vector3(-0.25f, 1.38f, 0), new Vector3(-0.28f, 1.13f, 0.05f), 0.07f, team);
            leftHand = art.Orb("LeftHand", modelRoot, new Vector3(-0.27f, 0.91f, 0.12f), new Vector3(0.10f, 0.15f, 0.07f), ArcadeArt.Skin);
            glove = art.Orb("Glove", modelRoot, new Vector3(-0.27f, 0.96f, 0.18f), new Vector3(0.20f, 0.25f, 0.15f), new Color(0.39f, 0.24f, 0.13f));
            glove.gameObject.SetActive(!helmet);
            leftRest = leftShoe.localPosition; rightRest = rightShoe.localPosition;
        }

        public void SetFirstPerson(bool firstPerson)
        {
            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>())
                renderer.enabled = !firstPerson || renderer.transform == leftHand || renderer.transform == rightHand;
        }

        public void PresentPitch(float windup, bool windingUp)
        {
            arm.localRotation = Quaternion.Euler(windingUp ? -360 * windup : 0, 0, 0);
            float lift = windingUp ? Mathf.Sin(windup * Mathf.PI) : 0;
            modelRoot.localPosition = new Vector3(0, lift * 0.10f, 0);
            modelRoot.localRotation = Quaternion.Euler(0, windingUp ? Mathf.Sin(windup * Mathf.PI) * -16 : 0, 0);
            leftShoe.localPosition = leftRest + new Vector3(0, lift * 0.20f, lift * -0.13f);
        }

        public void PresentIdle(float dt)
        {
            idleTime += dt;
            modelRoot.localPosition = Vector3.up * (0.025f * Mathf.Sin(idleTime * 2.4f));
            modelRoot.localRotation = Quaternion.Euler(0, Mathf.Sin(idleTime * 0.8f) * 3, 0);
        }

        public void PresentBat(Vector3 grip, Vector3 tip, bool hasSwung)
        {
            float angle = Mathf.Atan2(tip.z - grip.z, tip.x - grip.x) * Mathf.Rad2Deg;
            modelRoot.localRotation = Quaternion.Euler(0, hasSwung ? -angle * 0.28f : 0, 0);
            leftHand.position = grip + new Vector3(0, -0.025f, 0);
            rightHand.position = grip + (tip - grip).normalized * 0.11f;
            rightShoe.localPosition = rightRest + new Vector3(0, 0, hasSwung ? -0.06f : 0);
        }
    }
}
