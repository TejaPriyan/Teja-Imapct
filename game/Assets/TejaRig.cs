using UnityEngine;

// Prototype skeleton and hand-authored motion. The supplied GLB contained no rig or clips.
public class TejaRig : MonoBehaviour {
    public Transform[] bones;
    public Vector3[] restPositions;
    public Transform socket;
    public Animation animationPlayer;
    public static readonly string[] Names = {"Hips", "Spine", "Chest", "Neck", "Head", "UpperArmL", "ForearmL", "HandL", "UpperArmR", "ForearmR", "HandR", "ThighL", "ShinL", "FootL", "ThighR", "ShinR", "FootR", "CapeTop", "CapeMid", "CapeEnd"};
    string active = "";
    float locomotionSpeed = 4.2f;
    int triggerSerial,networkTriggerSerial=-1;
    public string CurrentState { get { return active; } }
    public int TriggerSerial { get { return triggerSerial; } }

    public void Play(string state) {
        if (!animationPlayer || !animationPlayer.GetClip(state)) return;
        if (state == active && animationPlayer.IsPlaying(state)) { ApplyCadence(state); return; }
        float phase = 0;
        bool preserveStride = IsMoving(active) && IsMoving(state) && animationPlayer[active] != null;
        if (preserveStride) phase = Mathf.Repeat(animationPlayer[active].normalizedTime, 1);
        active = state;
        animationPlayer.CrossFade(state, Loops(state) ? .18f : .10f);
        if (preserveStride) animationPlayer[state].normalizedTime = phase;
        ApplyCadence(state);
    }
    public void Trigger(string state) {
        if (!animationPlayer || !animationPlayer.GetClip(state)) return;
        triggerSerial = triggerSerial == int.MaxValue ? 1 : triggerSerial + 1;
        // CrossFade alone does not restart an already playing legacy AnimationState.
        if (animationPlayer.IsPlaying(state)) animationPlayer.Stop(state);
        active = state;
        animationPlayer[state].speed = 1;
        animationPlayer[state].time = 0;
        animationPlayer.CrossFade(state, state == "Hurt" || state == "Shoot" ? .045f : .075f);
        animationPlayer[state].time = 0;
    }
    // A new action serial restarts repeated shots. Repeated snapshots leave the clip
    // running; locomotion snapshots consume the serial without replaying an old action.
    public bool ApplyNetworkState(string state, int serial) {
        state = NetworkAnimation(state);
        serial = Mathf.Max(0, serial);
        bool fresh = serial != networkTriggerSerial;
        networkTriggerSerial = serial;
        if (fresh && serial > 0 && !Loops(state) && animationPlayer && animationPlayer.GetClip(state)) {
            Trigger(state);
            return true;
        }
        Play(state);
        return false;
    }
    public static string NetworkAnimation(string state) {
        switch (state) {
            case "Idle": case "Run": case "Sprint": case "GunIdle": case "GunRun":
            case "Jump": case "Land": case "Sword": case "Sword2": case "Sword3":
            case "Shoot": case "Reload": case "Dodge": case "Hurt": case "Death":
                return state;
            default: return "Idle";
        }
    }
    // Metres per second; match cadence to controller motion without moving the root twice.
    public void SetLocomotionSpeed(float metresPerSecond) {
        locomotionSpeed = Mathf.Max(0, metresPerSecond);
        ApplyCadence(active);
    }
    void ApplyCadence(string state) {
        if (!animationPlayer || string.IsNullOrEmpty(state) || animationPlayer[state] == null) return;
        animationPlayer[state].speed = IsMoving(state) ? Mathf.Clamp(locomotionSpeed / (state == "Sprint" ? 6.8f : 4.2f), .65f, 1.55f) : 1;
    }
    static bool IsMoving(string state) { return state == "Run" || state == "GunRun" || state == "Sprint"; }
    public static bool Loops(string state) { return state == "Idle" || state == "GunIdle" || IsMoving(state); }
    public static float Duration(string state) {
        switch (state) {
            case "Run": case "GunRun": return .72f;
            case "Sprint": return .56f;
            case "Sword": return .62f;
            case "Sword2": return .66f;
            case "Sword3": return .78f;
            case "Reload": return 1.25f;
            case "Shoot": return .28f;
            case "Dodge": return .45f;
            case "Jump": return .8f;
            case "Land": return .24f;
            case "Hurt": return .4f;
            case "Death": return 1.1f;
            default: return 2.4f;
        }
    }
    public void Pose(string state, float time) {
        if (bones == null || bones.Length < Names.Length) return;
        for (int i = 0; i < bones.Length; i++) {
            bones[i].localRotation = Quaternion.identity;
            if (restPositions != null && restPositions.Length == bones.Length) bones[i].localPosition = restPositions[i];
        }
        float u = Mathf.Clamp01(time / Duration(state)), cycle = u * Mathf.PI * 2;
        float s = Mathf.Sin(cycle);
        // Relaxed A-pose conversion, soft elbows and a small breathing/weight-shift cycle.
        Set(5, -3, 0, 43); Set(8, -3, 0, -43);
        Set(6, -13, 0, 0); Set(9, -13, 0, 0);
        Set(0, 0, s * .6f, s * .7f); Set(1, s * .9f, 0, -s * .5f);
        Set(2, -s * .6f, 0, 0); Set(4, 0, s * 1.1f, 0);
        HipOffset(s * .004f, s * .004f, 0);
        Cape(cycle, 1.5f, 1.8f, 0);

        bool running = IsMoving(state), sprint = state == "Sprint";
        if (running) {
            float lean = sprint ? 10 : 5, stride = sprint ? 42 : 33;
            Set(0, lean, s * 4, s * 2.1f); Set(1, -lean * .58f, -s * 5, -s * 1.5f);
            Set(2, -2, -s * 2.5f, 0); Set(4, -lean * .25f, s * 2.6f, 0);
            HipOffset(s * .012f, -.039f + Mathf.Cos(cycle * 2) * (sprint ? .061f : .048f), 0);
            Leg(11, cycle, stride, lean, sprint);
            Leg(14, cycle + Mathf.PI, stride, lean, sprint);
            float armSwing = sprint ? .25f : .20f;
            AimArm(8, new Vector3(.38f, 1.44f, -.10f), new Vector3(.24f, 1.32f + Mathf.Max(0, s) * .075f, .045f + s * armSwing), -48);
            AimArm(5, new Vector3(-.38f, 1.44f, -.10f), new Vector3(-.24f, 1.32f + Mathf.Max(0, -s) * .075f, .045f - s * armSwing), 48);
            Cape(cycle, sprint ? 18 : 11, sprint ? 6 : 4, s * 3);
        }
        if (state == "GunIdle" || state == "GunRun" || state == "Shoot" || state == "Reload") {
            // Fast kick, damped return. Hand rolls preserve the weapon socket convention.
            float recoil = state == "Shoot" ? Pulse(u, .13f, .65f) : 0;
            float reload = state == "Reload" ? Window(u, .08f, .25f, .72f, 1) : 0;
            Set(1, -recoil * 3 + reload * 3, 5, 0); Set(2, 0, -2, 0);
            Set(4, reload * 9, -3, 0);
            float bob = running ? Mathf.Cos(cycle * 2) * .007f : s * .003f;
            Vector3 right = new Vector3(.19f, 1.51f + bob + recoil * .017f, .40f - recoil * .038f);
            Vector3 left = new Vector3(.03f, 1.50f + bob, .40f - recoil * .038f);
            right += new Vector3(0, -.10f, -.055f) * reload;
            if (state == "Reload") {
                // Support hand leaves the foregrip, fetches a cell, seats it, then returns.
                left = Track(u, left, new Vector3(-.15f, 1.28f, .08f), new Vector3(.08f, 1.35f, .29f), new Vector3(.05f, 1.42f, .35f), left);
            }
            AimArm(8, new Vector3(.37f, 1.38f, .13f), right, -48, reload * 13 - recoil * 3);
            AimArm(5, new Vector3(-.30f, 1.36f, .13f), left, 42, reload * -12);
        }
        if (state == "Sword" || state == "Sword2" || state == "Sword3") SwordPose(state, u, cycle);
        if (state == "Jump") {
            float tuck = Mathf.Sin(u * Mathf.PI);
            Set(0, 4, 0, -2); Set(1, -5, 0, 0);
            Set(11, -18 - tuck * 18, 0, -3); Set(14, 7 - tuck * 7, 0, 3);
            Set(12, 25 + tuck * 42, 0, 0); Set(15, 20 + tuck * 33, 0, 0);
            Set(13, -12, 0, 0); Set(16, -12, 0, 0);
            Set(5, -22, 0, 28); Set(8, -22, 0, -28);
            Set(6, -34, 0, 0); Set(9, -34, 0, 0);
            Cape(cycle, 17 + tuck * 5, 3, 0);
        }
        if (state == "Land") {
            float settle = 1 - Mathf.SmoothStep(0, 1, u);
            HipOffset(0, -.10f * settle, 0);
            Set(0, 5 * settle, 0, 0); Set(1, 4 * settle, 0, 0);
            Set(11, -24 * settle, 0, -2 * settle); Set(14, -24 * settle, 0, 2 * settle);
            Set(12, 43 * settle, 0, 0); Set(15, 43 * settle, 0, 0);
            Set(13, -24 * settle, 0, 0); Set(16, -24 * settle, 0, 0);
            Set(5, -12 * settle, 0, 43 - 8 * settle); Set(8, -12 * settle, 0, -43 + 8 * settle);
            Cape(cycle, 12 * settle, 2 * settle, 0);
        }
        if (state == "Dodge") {
            float crouch = Window(u, 0, .15f, .64f, 1);
            Set(0, 8 + crouch * 18, 0, 0); Set(1, 6 * crouch, 0, 0);
            HipOffset(0, -crouch * .17f, 0);
            Set(11, -42 * crouch, 0, 0); Set(12, 70 * crouch, 0, 0);
            Set(14, 20 * crouch, 0, 0); Set(15, 20 * crouch, 0, 0);
            Set(5, 26 * crouch, 0, 34); Set(8, 26 * crouch, 0, -34);
            Set(6, -40, 0, 0); Set(9, -40, 0, 0); Cape(cycle, 28 * crouch, 3, 0);
        }
        if (state == "Hurt") {
            float hurt = Pulse(u, .18f, 1);
            Set(1, -13 * hurt, 0, 6 * hurt); Set(4, 8 * hurt, 0, 0);
            Set(6, -13 - 20 * hurt, 0, 0); Set(9, -13 - 20 * hurt, 0, 0);
            HipOffset(0, -.025f * hurt, -.025f * hurt);
        }
        if (state == "Death") {
            float collapse = Mathf.SmoothStep(0, 1, u);
            HipOffset(0, -.58f * collapse, -.10f * collapse);
            Set(0, -48 * collapse, 0, 13 * collapse); Set(1, 12 * collapse, 0, 0);
            Set(4, 23 * collapse, -13 * collapse, 0);
            Set(11, -25 * collapse, 0, -5 * collapse); Set(14, -25 * collapse, 0, 5 * collapse);
            Set(12, 70 * collapse, 0, 0); Set(15, 70 * collapse, 0, 0);
            Set(5, -12 * collapse, 0, 43 - 17 * collapse); Set(8, -12 * collapse, 0, -43 + 17 * collapse);
        }
    }
    void Leg(int thigh, float phase, float stride, float lean, bool sprint) {
        float swing = Mathf.Sin(phase);
        float upper = 3 - swing * stride;
        float knee = 7 + Mathf.Pow(Mathf.Max(0, -Mathf.Sin(phase - .30f)), 1.25f) * (sprint ? 86 : 72);
        Set(thigh, upper, 0, thigh == 11 ? -1.3f : 1.3f);
        Set(thigh + 1, knee, 0, 0);
        // Plant the boot during contact; let the toe trail during recovery.
        float toe = -Mathf.Max(0, -swing) * 18;
        Set(thigh + 2, -lean - upper - knee + toe, 0, 0);
    }
    void SwordPose(string state, float u, float cycle) {
        Vector3 rest = new Vector3(-3, 0, -43), windup, contact, follow;
        float turn, dip;
        if (state == "Sword2") {
            windup = new Vector3(14, -79, -25); contact = new Vector3(-78, 32, -45); follow = new Vector3(-121, 51, -38);
            turn = -1; dip = .035f;
        } else if (state == "Sword3") {
            windup = new Vector3(-147, -9, -30); contact = new Vector3(-75, 8, -35); follow = new Vector3(-22, -10, -27);
            turn = .35f; dip = .09f;
        } else {
            windup = new Vector3(-116, 28, -29); contact = new Vector3(-70, -59, -30); follow = new Vector3(-12, -85, -24);
            turn = 1; dip = .045f;
        }
        float action = Window(u, 0, .22f, .70f, 1);
        float impact = Pulse(Mathf.InverseLerp(.22f, .78f, u), .46f, 1);
        float twist = Track(u, Vector3.zero, Vector3.up * (-19 * turn), Vector3.up * (23 * turn), Vector3.up * (31 * turn), Vector3.zero).y;
        Set(8, Track(u, rest, windup, contact, follow, rest));
        Set(9, Track(u, new Vector3(-13, 0, 0), new Vector3(-54, 0, 0), new Vector3(-17, 0, 0), new Vector3(-25, 0, 0), new Vector3(-13, 0, 0)));
        Set(0, impact * 6, twist * .25f, -impact * 2);
        Set(1, (state == "Sword3" ? 15 : 5) * impact, twist * .65f, 0);
        Set(2, -3 * action, twist * .30f, 0); Set(4, -3 * impact, -twist * .35f, 0);
        Set(5, -18 * action, -twist * .35f, 43 - action * 13); Set(6, -13 - action * 32, 0, 0);
        Set(11, -12 * action, 0, -action * 3); Set(12, 20 * action, 0, 0); Set(13, -8 * action, 0, 0);
        Set(14, 10 * action, 0, action * 4); Set(15, 7 * action, 0, 0); Set(16, -17 * action, 0, 0);
        HipOffset(-turn * action * .018f, -dip * action, impact * .035f);
        Cape(cycle, action * 14, action * 3, -twist * .18f);
    }
    void Cape(float cycle, float trail, float flutter, float twist) {
        // Positive pitch moves a downward hanging cape behind the character (+Z is forward).
        Set(17, trail + Mathf.Sin(cycle - .5f) * flutter * .45f, twist * .35f, Mathf.Sin(cycle - .3f) * flutter * .25f);
        Set(18, trail * .55f + Mathf.Sin(cycle - 1.1f) * flutter, twist * .45f, 0);
        Set(19, trail * .35f + Mathf.Sin(cycle - 1.8f) * flutter * 1.4f, twist * .6f, Mathf.Sin(cycle - 1.4f) * flutter * .4f);
    }
    static float Pulse(float u, float peak, float end) {
        return u < peak ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, peak, u)) : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(peak, end, u));
    }
    static float Window(float u, float start, float rise, float fall, float end) {
        return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, rise, u)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(fall, end, u)));
    }
    static Vector3 Track(float u, Vector3 start, Vector3 windup, Vector3 contact, Vector3 follow, Vector3 end) {
        if (u < .22f) return Vector3.Lerp(start, windup, Mathf.SmoothStep(0, 1, u / .22f));
        if (u < .48f) return Vector3.Lerp(windup, contact, Mathf.SmoothStep(0, 1, (u - .22f) / .26f));
        if (u < .70f) return Vector3.Lerp(contact, follow, Mathf.SmoothStep(0, 1, (u - .48f) / .22f));
        return Vector3.Lerp(follow, end, Mathf.SmoothStep(0, 1, (u - .70f) / .30f));
    }
    void Set(int i, float x, float y, float z) { bones[i].localRotation = Quaternion.Euler(x, y, z); }
    void Set(int i, Vector3 angles) { bones[i].localRotation = Quaternion.Euler(angles); }
    void HipOffset(float x, float y, float z) {
        if (restPositions != null && restPositions.Length == bones.Length) bones[0].localPosition = restPositions[0] + new Vector3(x, y, z);
    }
    void AimArm(int upper, Vector3 elbowHint, Vector3 wrist, float handRoll, float handPitch = 0) {
        Transform shoulder = bones[upper], elbow = bones[upper + 1], hand = bones[upper + 2];
        Vector3 origin = shoulder.position, target = transform.TransformPoint(wrist);
        float upperLength = shoulder.TransformVector(elbow.localPosition).magnitude;
        float lowerLength = elbow.TransformVector(hand.localPosition).magnitude;
        Vector3 toTarget = target - origin;
        float distance = toTarget.magnitude;
        if (distance < .0001f || upperLength < .0001f || lowerLength < .0001f) return;
        Vector3 direction = toTarget / distance;
        // A small bend remains even at full reach; neither segment is translated or stretched.
        distance = Mathf.Clamp(distance, Mathf.Abs(upperLength - lowerLength) + .005f, (upperLength + lowerLength) * .975f);
        target = origin + direction * distance;
        Vector3 bend = Vector3.ProjectOnPlane(transform.TransformPoint(elbowHint) - origin, direction);
        if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(transform.right * (upper == 8 ? 1 : -1), direction);
        if (bend.sqrMagnitude < .000001f) bend = Vector3.Cross(direction, transform.forward);
        bend.Normalize();
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
        float height = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
        Vector3 targetElbow = origin + direction * along + bend * height;
        // Apply world-space deltas to current world rotations, preserving parent/root rotation.
        shoulder.rotation = Quaternion.FromToRotation(elbow.position - origin, targetElbow - origin) * shoulder.rotation;
        elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, target - elbow.position) * elbow.rotation;
        hand.rotation = transform.rotation * Quaternion.Euler(handPitch, 0, handRoll);
    }
}
