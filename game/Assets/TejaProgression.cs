using UnityEngine;
using System;

// Small, persistent quest state machines. Reward state is committed before payment.
public partial class TejaAdventure {
    const int LevelCap=20;
    int playerLevel=1,experience,skillPoints,vitalityRank,powerRank;
    int[] weaponRanks={0,0,0};
    // 0: available, 1: accepted, 2: return for reward, 3: completed.
    int[] missionStates={0,0,0};
    int commissionDefeats,dialogueNpc=-1,trackedMission=-1;
    bool relicRecovered;
    Transform relicRoot,relicCrystal;
    readonly Transform[] questNpcs=new Transform[3],questMarkers=new Transform[3];
    readonly string[] npcNames={"Mira","Ren","Ilyra"};
    readonly string[] npcRoles={"Village steward","Stranded scout","Relic scholar"};
    readonly string[] missionNames={"A safer tomorrow","The missing scout","Echoes in the stone"};
    readonly int[] missionCoins={100,140,180},missionXp={150,190,240};
    readonly Vector3[] npcLocations={new Vector3(2,0,31),new Vector3(112,0,-12),new Vector3(-108,0,-54)};
    readonly Vector3 relicLocation=new Vector3(-123,0,-70);
    float MaxHealth {get{return 100+4*(playerLevel-1)+5*vitalityRank;}}
    float DamageMultiplier {get{return 1+.02f*(playerLevel-1)+.05f*powerRank+.15f*weaponRanks[Mathf.Clamp(equippedWeapon,0,2)];}}
    int ExperienceToNextLevel {get{return 90+playerLevel*35;}}
    int CompletedMissions {get{int n=0;foreach(int state in missionStates)if(state==3)n++;return n;}}

    [Serializable] class ProgressionSave {
        public int level=1,xp,points,vitality,power,commissionKills,tracked=-1;
        public int[] ranks=new int[3],missions=new int[3];
        public bool relic;
    }
    ProgressionSave GetProgressionSave(){
        return new ProgressionSave{level=playerLevel,xp=experience,points=skillPoints,vitality=vitalityRank,power=powerRank,
            commissionKills=commissionDefeats,tracked=trackedMission,ranks=(int[])weaponRanks.Clone(),missions=(int[])missionStates.Clone(),relic=relicRecovered};
    }
    void ApplyProgressionSave(ProgressionSave data){
        ResetProgression();if(data==null)return;
        playerLevel=Mathf.Clamp(data.level,1,LevelCap);
        experience=playerLevel==LevelCap?0:Mathf.Clamp(data.xp,0,ExperienceToNextLevel-1);
        vitalityRank=Mathf.Clamp(data.vitality,0,Mathf.Min(5,playerLevel-1));
        powerRank=Mathf.Clamp(data.power,0,Mathf.Min(5,playerLevel-1-vitalityRank));
        skillPoints=Mathf.Clamp(data.points,0,playerLevel-1-vitalityRank-powerRank);
        for(int i=0;i<3;i++){
            weaponRanks[i]=data.ranks!=null&&i<data.ranks.Length?Mathf.Clamp(data.ranks[i],0,3):0;
            missionStates[i]=data.missions!=null&&i<data.missions.Length?Mathf.Clamp(data.missions[i],0,3):0;
        }
        commissionDefeats=Mathf.Clamp(data.commissionKills,0,6);
        if(missionStates[0]==0)commissionDefeats=0;
        if(missionStates[0]==1&&commissionDefeats>=6)missionStates[0]=2;
        if(missionStates[0]==2)commissionDefeats=6;
        relicRecovered=data.relic&&missionStates[2]>0;
        if(missionStates[2]>=2)relicRecovered=true;
        if(relicRecovered&&missionStates[2]==1)missionStates[2]=2;
        trackedMission=Mathf.Clamp(data.tracked,-1,2);dialogueNpc=-1;
        RefreshQuestMarkers();
    }
    void BuildProgression(){
        var actors=G("People of Aetherfall");
        for(int i=0;i<3;i++){
            Vector3 p=npcLocations[i];p.y=GroundHeight(p)+.06f;
            var root=G(npcNames[i]+" — "+npcRoles[i],actors,p);questNpcs[i]=root;
            root.localRotation=Quaternion.Euler(0,i==0?180:i==1?215:45,0);
            string coat=i==0?"#b96d66":i==1?"#517d79":"#77668f";
            Cylinder("#879f91",root,V(0,.035f,0),V(1.45f,.035f,1.45f));
            var torso=Shape(PrimitiveType.Capsule,coat,root,V(0,1.02f,0),V(.62f,.49f,.4f));
            Box("#e7cf9c",root,V(0,1.06f,.22f),V(.63f,.085f,.05f));
            Ball("#ddb494",root,V(0,1.75f,.015f),V(.44f,.5f,.42f));
            Ball(i==2?"#c9d0cc":"#45434b",root,V(0,1.91f,-.035f),V(.48f,.27f,.43f));
            foreach(int side in new[]{-1,1}){
                Box("#344553",root,V(side*.15f,.39f,0),V(.19f,.68f,.21f));
                Box("#273746",root,V(side*.15f,.09f,.07f),V(.23f,.18f,.38f));
                var arm=Shape(PrimitiveType.Capsule,coat,root,V(side*.38f,1.12f,0),V(.18f,.34f,.20f));
                arm.localRotation=Quaternion.Euler(i==2?-18:0,0,side*8);
                Ball("#ddb494",root,V(side*.41f,.79f,.025f),V(.17f,.20f,.17f));
                Box("#233d49",root,V(side*.085f,1.77f,.215f),V(.045f,.04f,.026f));
            }
            if(i==0){Box("#e5bd6d",root,V(0,1.53f,.18f),V(.42f,.1f,.2f));Box("#b5c8ba",root,V(-.43f,.84f,.16f),V(.1f,.4f,.32f));}
            if(i==1){Box("#927c5c",root,V(0,1.2f,-.3f),V(.52f,.57f,.22f));Cylinder("#6b6555",root,V(.43f,.68f,.13f),V(.07f,.68f,.07f));}
            if(i==2){Box("#d2c49f",root,V(-.35f,.9f,.27f),V(.39f,.08f,.38f));Cylinder("#e1c392",root,V(.43f,1.03f,.05f),V(.065f,1.03f,.065f));Ball("#a4ded6",root,V(.43f,2.12f,.05f),V(.26f,.26f,.26f));}
            questMarkers[i]=Box("#f1d18b",root,V(0,2.65f,0),V(.26f,.26f,.26f));
            questMarkers[i].localRotation=Quaternion.Euler(0,45,45);
        }
        Vector3 relicAt=relicLocation;relicAt.y=GroundHeight(relicAt);
        relicRoot=G("Starfall memory relic",null,relicAt);
        Cylinder("#a6b7aa",relicRoot,V(0,.2f,0),V(1.7f,.2f,1.7f));
        Cylinder("#778f8b",relicRoot,V(0,.63f,0),V(.55f,.43f,.55f));
        relicCrystal=Box("#b9eced",relicRoot,V(0,1.4f,0),V(.5f,.5f,.5f));
        relicCrystal.localRotation=Quaternion.Euler(35,45,25);
        ResetProgression();
    }
    void ResetProgression(){
        playerLevel=1;experience=skillPoints=vitalityRank=powerRank=commissionDefeats=0;
        weaponRanks=new int[3];missionStates=new int[3];relicRecovered=false;trackedMission=dialogueNpc=-1;
        RefreshQuestMarkers();
    }
    void RefreshQuestMarkers(){
        for(int i=0;i<3;i++)if(questMarkers[i]){
            questMarkers[i].gameObject.SetActive(missionStates[i]!=3);
            questMarkers[i].GetComponent<Renderer>().sharedMaterial=M(missionStates[i]==2?"#9ce4d8":missionStates[i]==0?"#f1d18b":"#d2dbe5");
        }
        if(relicRoot)relicRoot.gameObject.SetActive(missionStates[2]==1&&!relicRecovered);
    }
    void TickProgression(float dt){
        for(int i=0;i<3;i++)if(questMarkers[i]){
            questMarkers[i].Rotate(0,50*dt,0,Space.World);
            questMarkers[i].localPosition=V(0,2.65f+Mathf.Sin(elapsed*2+i)*.13f,0);
        }
        if(relicCrystal){relicCrystal.Rotate(0,48*dt,0,Space.World);relicCrystal.localPosition=V(0,1.4f+Mathf.Sin(elapsed*2)*.12f,0);}
        if(missionStates[1]==1&&ScoutThreats()==0){missionStates[1]=2;RefreshQuestMarkers();Notify("The scout is safe. Speak to Ren to finish the rescue.");SaveJourney(false);}
    }
    int ScoutThreats(){
        int alive=0;foreach(var foe in foes)if(!foe.dead&&foe.active&&(FlatDistance(foe.spawn,npcLocations[1])<24||FlatDistance(foe.root.position,npcLocations[1])<15))alive++;
        return alive;
    }
    int NearbyNpc(){
        int best=-1;float nearest=3.7f;
        for(int i=0;i<3;i++)if(questNpcs[i]){float d=FlatDistance(hero.position,questNpcs[i].position);if(d<nearest&&Mathf.Abs(hero.position.y-questNpcs[i].position.y)<3){nearest=d;best=i;}}
        return best;
    }
    string NearbyProgressionPrompt(){
        if(!hero)return "";
        if(relicRoot&&relicRoot.gameObject.activeSelf&&FlatDistance(hero.position,relicRoot.position)<3)return "Recover the memory relic";
        int i=NearbyNpc();return i<0?"":"Talk to "+npcNames[i]+(missionStates[i]==2?" / claim reward":"");
    }
    bool InteractProgression(){
        if(phase!=Phase.Play)return false;
        if(relicRoot&&relicRoot.gameObject.activeSelf&&missionStates[2]==1&&FlatDistance(hero.position,relicRoot.position)<3){
            relicRecovered=true;missionStates[2]=2;trackedMission=2;RefreshQuestMarkers();
            PulseAt(relicRoot.position,"#bcece1",3,.65f);Tone(920,.3f);Notify("Memory relic recovered. Return it to Ilyra.");SaveJourney(false);return true;
        }
        int npc=NearbyNpc();if(npc<0)return false;
        dialogueNpc=npc;phase=Phase.Dialogue;stick=Vector2.zero;stickFinger=lookFinger=-1;return true;
    }
    void AcceptMission(int id){if(CoopIsGuest){QueueCoopCommand("accept",id);return;}
        if(id<0||id>=3||missionStates[id]!=0)return;
        missionStates[id]=1;trackedMission=id;
        if(id==0)commissionDefeats=0;
        if(id==1&&ScoutThreats()==0)missionStates[id]=2;
        RefreshQuestMarkers();Notify("Quest accepted  ·  "+missionNames[id]);SaveJourney(false);
    }
    void CompleteMission(int id){if(CoopIsGuest){QueueCoopCommand("complete",id);return;}
        if(id<0||id>=3||missionStates[id]!=2)return;
        if(id==0&&commissionDefeats<6)return;
        if(id==1&&ScoutThreats()>0){missionStates[1]=1;RefreshQuestMarkers();return;}
        if(id==2&&!relicRecovered)return;
        missionStates[id]=3; // Set the terminal state before awarding anything.
        AddCoins(missionCoins[id]);AwardExperience(missionXp[id]);
        if(id==1)potions=Mathf.Min(9,potions+1);
        if(trackedMission==id){trackedMission=-1;for(int i=0;i<3;i++)if(missionStates[i]==1||missionStates[i]==2){trackedMission=i;break;}}
        RefreshQuestMarkers();Notify("Quest complete  ·  +"+missionCoins[id]+" coins  ·  +"+missionXp[id]+" XP"+(id==1?"  ·  +1 tonic":""));
        SaveJourney(false);Tone(880,.35f);
    }
    void RecordDefeat(Foe foe){
        if(foe==null)return;
        AwardExperience(foe.boss?220:35);
        if(missionStates[0]==1){commissionDefeats=Mathf.Min(6,commissionDefeats+1);if(commissionDefeats==6){missionStates[0]=2;RefreshQuestMarkers();Notify("Commission complete. Return to Mira in Lantern Village.");}}
    }
    void AwardExperience(int amount){
        if(amount<=0||playerLevel>=LevelCap)return;
        experience=(int)Math.Min(100000L,(long)experience+amount);int before=playerLevel;
        while(playerLevel<LevelCap&&experience>=ExperienceToNextLevel){experience-=ExperienceToNextLevel;playerLevel++;skillPoints++;hp=Mathf.Min(MaxHealth,hp+12);}
        if(playerLevel==LevelCap)experience=0;
        if(playerLevel>before){Notify("Level "+playerLevel+"  ·  +"+(playerLevel-before)+" skill point"+(playerLevel-before>1?"s":"")+"  ·  C to improve Teja");Tone(1040,.3f);}
    }
    int WeaponUpgradeCost(int id){return id<0||id>=3||weaponRanks[id]>=3?0:new[]{90,160,260}[weaponRanks[id]];}
    bool UpgradeWeapon(int id){if(CoopIsGuest){QueueCoopCommand("upgrade",id);return false;}
        if(id<0||id>=3||(ownedWeapons&(1<<id))==0){Notify("Own a weapon before improving it.");return false;}
        if(weaponRanks[id]>=3){Notify("This weapon is at its maximum rank.");return false;}
        int cost=WeaponUpgradeCost(id);if(coins<cost){Notify("You need "+(cost-coins)+" more earned coins.");return false;}
        coins-=cost;weaponRanks[id]++;Notify(weapons[id].name+" improved to rank "+weaponRanks[id]+"  ·  +15% base damage");SaveJourney(false);Tone(760,.2f);return true;
    }
    bool SpendSkillPoint(bool vitality){if(CoopIsGuest){QueueCoopCommand("skill",vitality?1:0);return false;}
        if(skillPoints<=0){Notify("Earn a level to gain a skill point.");return false;}
        if((vitality?vitalityRank:powerRank)>=5){Notify("This talent is already at rank 5.");return false;}
        skillPoints--;if(vitality){vitalityRank++;hp=Mathf.Min(MaxHealth,hp+5);}else powerRank++;
        Notify(vitality?"Vitality improved  ·  +5 maximum health":"Focus improved  ·  +5% base damage");SaveJourney(false);return true;
    }
    string MissionObjective(int id){
        if(missionStates[id]==3)return "Completed. Reward received.";
        if(missionStates[id]==0)return "Speak to "+npcNames[id]+" / "+(id==0?"Lantern Village":id==1?"Silverrun River":"western outer ruins");
        if(missionStates[id]==2)return "Return to "+npcNames[id]+" to claim your reward.";
        if(id==0)return "Defeat rift foes after accepting  /  "+commissionDefeats+" of 6";
        if(id==1)return "Clear threats around Ren  /  "+ScoutThreats()+" remaining";
        return "Recover the memory relic in the western outer ruins.";
    }
    Vector3 MissionDestination(int id){return id==2&&missionStates[id]==1?relicLocation:npcLocations[id];}
    void ValidateProgression(){
        // Exercise complete mission journeys and refusal paths without touching disk in smoke mode.
        ProgressionSave previous=GetProgressionSave();int priorCoins=coins,priorOwned=ownedWeapons,priorPotions=potions,priorEquipped=equippedWeapon;
        float priorHp=hp;Phase priorPhase=phase;Vector3 priorPosition=hero.position;string priorNotice=notice;float priorNoticeTime=noticeTime;
        bool[] dead=new bool[foes.Count];for(int i=0;i<foes.Count;i++)dead[i]=foes[i].dead;
        try{
            ResetProgression();coins=0;ownedWeapons=1;equippedWeapon=0;phase=Phase.Play;
            Require(questNpcs[0]&&questNpcs[1]&&questNpcs[2]&&relicRoot,"Three NPC contacts and relic built");
            motor.enabled=false;hero.position=questNpcs[0].position+V(0,0,-2);motor.enabled=true;
            Require(InteractProgression()&&phase==Phase.Dialogue&&dialogueNpc==0,"Village NPC opens dialogue");
            CompleteMission(0);Require(coins==0&&missionStates[0]==0,"Unaccepted quest cannot award rewards");
            AcceptMission(0);RecordDefeat(foes[0]);AcceptMission(0);Require(commissionDefeats==1,"Repeated accept does not reset progress");
            CompleteMission(0);Require(coins==0&&missionStates[0]==1,"Incomplete quest cannot pay");
            for(int i=1;i<6;i++)RecordDefeat(foes[i]);
            Require(missionStates[0]==2&&commissionDefeats==6,"Six defeats satisfy village mission");
            CompleteMission(0);int paidXp=experience,paidLevel=playerLevel;CompleteMission(0);
            Require(coins==100&&missionStates[0]==3&&experience==paidXp&&playerLevel==paidLevel,"Village mission pays once");
            AcceptMission(1);int threats=ScoutThreats();Require(threats>0&&missionStates[1]==1,"Scout begins surrounded by real threats");
            CompleteMission(1);Require(coins==100,"Scout reward blocked while threatened");
            foreach(var foe in foes)if(FlatDistance(foe.spawn,npcLocations[1])<24||FlatDistance(foe.root.position,npcLocations[1])<15)foe.dead=true;
            TickProgression(0);Require(missionStates[1]==2,"Scout rescue becomes ready after clearing threats");
            CompleteMission(1);CompleteMission(1);Require(coins==240&&missionStates[1]==3,"Scout rescue pays once");
            AcceptMission(2);Require(relicRoot.gameObject.activeSelf,"Accepting scholar quest reveals relic");
            CompleteMission(2);Require(coins==240,"Scholar reward requires relic");
            phase=Phase.Play;motor.enabled=false;hero.position=relicRoot.position+V(0,0,2);motor.enabled=true;
            Require(InteractProgression()&&relicRecovered&&missionStates[2]==2&&!relicRoot.gameObject.activeSelf,"Relic pickup advances return objective");
            Require(coins==240,"Relic pickup itself does not award turn-in coins");
            motor.enabled=false;hero.position=questNpcs[2].position+V(0,0,-2);motor.enabled=true;
            Require(InteractProgression()&&dialogueNpc==2,"Return to scholar opens reward conversation");
            CompleteMission(2);CompleteMission(2);Require(coins==420&&CompletedMissions==3,"All mission rewards paid exactly once");
            var roundTrip=JsonUtility.FromJson<ProgressionSave>(JsonUtility.ToJson(GetProgressionSave()));ResetProgression();ApplyProgressionSave(roundTrip);
            Require(CompletedMissions==3&&relicRecovered&&commissionDefeats==6,"Quest state survives serialized round-trip");
            CompleteMission(0);CompleteMission(1);CompleteMission(2);Require(coins==420,"Loaded completed quests cannot repay");
            coins=0;Require(!UpgradeWeapon(0)&&weaponRanks[0]==0&&coins==0,"Weapon upgrade affordability gate");
            coins=9999;Require(!UpgradeWeapon(1)&&coins==9999,"Unowned weapon upgrade rejected");ownedWeapons=7;
            Require(UpgradeWeapon(0)&&UpgradeWeapon(0)&&UpgradeWeapon(0),"Three paid weapon rank improvements");
            int afterRanks=coins;Require(afterRanks==9489&&!UpgradeWeapon(0)&&weaponRanks[0]==3&&coins==afterRanks,"Weapon rank cap and exact upgrade costs");
            ResetProgression();Require(!SpendSkillPoint(true)&&skillPoints==0,"No negative skill point spending");
            AwardExperience(int.MaxValue);Require(playerLevel==20&&experience==0&&skillPoints==19&&MaxHealth==176,"XP safely reaches level cap with earned skill points");
            for(int i=0;i<5;i++)Require(SpendSkillPoint(true),"Vitality point spend");
            Require(!SpendSkillPoint(true)&&vitalityRank==5&&skillPoints==14&&MaxHealth==201,"Vitality cap and maximum health growth");
            for(int i=0;i<5;i++)Require(SpendSkillPoint(false),"Focus point spend");
            Require(!SpendSkillPoint(false)&&powerRank==5&&skillPoints==9,"Focus talent cap");
            AwardExperience(9999);Require(playerLevel==20&&skillPoints==9&&experience==0,"Capped level cannot generate extra points");
            roundTrip=JsonUtility.FromJson<ProgressionSave>(JsonUtility.ToJson(GetProgressionSave()));ResetProgression();ApplyProgressionSave(roundTrip);
            Require(playerLevel==20&&vitalityRank==5&&powerRank==5&&skillPoints==9,"Character progression round-trip");
            ApplyProgressionSave(new ProgressionSave{level=-1,xp=-8,points=500,vitality=10,power=10,ranks=new[]{-1,22},missions=new[]{-2,8}});
            Require(playerLevel==1&&experience==0&&skillPoints==0&&vitalityRank==0&&powerRank==0&&weaponRanks[0]==0&&weaponRanks[1]==3&&missionStates[0]==0,"Malformed progression bounded safely");
        }finally{
            for(int i=0;i<foes.Count;i++)foes[i].dead=dead[i];
            ApplyProgressionSave(previous);coins=priorCoins;ownedWeapons=priorOwned;potions=priorPotions;equippedWeapon=priorEquipped;hp=priorHp;phase=priorPhase;
            motor.enabled=false;hero.position=priorPosition;motor.enabled=true;notice=priorNotice;noticeTime=priorNoticeTime;Physics.SyncTransforms();
        }
    }
    string DialogueText(int id){
        if(missionStates[id]==3)return id==0?"Our lanterns are burning bright again. You gave the village a little peace, Teja. Thank you.":id==1?"The road is clear, and I can finally get home. I'll make sure the village hears what you did.":"These markings remember a sky before the rift. You have given our forgotten history a voice.";
        if(missionStates[id]==2)return id==0?"Six fewer threats on our roads. That's a promise kept. Please, take the village's thanks.":id==1?"The last of them has fallen! I can walk out safely now. This is for you, and a tonic for the road.":"You found it! This is the memory relic I have been searching for. Let me reward your journey.";
        if(missionStates[id]==1)return id==0?"The rift creatures keep our families from the roads. Defeat six of them, then come back to me. Your journal will track your progress.":id==1?"Those creatures are still circling the camp. Clear the threats close to me, then speak to me once the way is safe.":"Look beyond me, farther west into the broken ruins. The memory relic glows a pale blue on a stone pedestal. Bring it back to me.";
        return id==0?"Welcome to Lantern Village. Our supply routes are overrun by rift creatures. Will you defeat six foes and help us make the roads safe again?":id==1?"Over here! I was scouting the river when the rift creatures surrounded my camp. Please clear the nearby threats so I can escape.":"I study the memories hidden in these ancient ruins. A pale blue relic lies farther west, among the broken stones. Recover it and bring it to me.";
    }
}
