﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
using Verse.AI;
namespace NewRatkin
{
    public class IncidentWorker_RatkinGuerrillaTunner : IncidentWorker
    {
        private const float tunnelPoints = 220f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            return base.CanFireNowSub(parms) && Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia).HostileTo(Faction.OfPlayer) && (RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2) && RatkinTunnelCellFinder.FindPowerPlantNearCell(out intVec, map);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;        //현재 대상 맵 =>C#문법 타입 변경
            Thing t = SpawnTunnels(map, parms); //맵 정보를 가지고 터널 생성 후 해당 터널을 캐치

            if (t != null) //터널 생성후 결과가 캐치된 터널이 없으면 이벤트 실행 불발
            {
                SendStandardLetter(t, null, new string[0]); //이벤트 알림 UI에 전달
                Find.TickManager.slower.SignalForceNormalSpeedShort();
                return true;
            }
            return false;       
        }
        private Thing SpawnTunnels(Map map, IncidentParms parms)
        {
            IntVec3 loc;
            if (!RatkinTunnelCellFinder.FindPowerPlantNearCell(out loc, map))   //발전 시설이 없다면 터널을 생성하지 않는다.
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnelSpawner, null), loc, map, WipeMode.FullRefund);
            ((GuerrillaTunnelSpawner)thing).eventPoint = parms.points;
            return thing;
        }
    }
}
