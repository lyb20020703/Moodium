using System;
using System.Linq;
using System.Reflection;
using Moodium.CandyWorld;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
public sealed class WorldDockIntegrationTests
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
 static void Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags).Invoke(o,a);
 sealed class Fixture:IDisposable
 {
  public readonly GameObject Root=new("Dock fixture");public readonly Camera Camera;public readonly WorldActionDock Dock;
  public Fixture(){Camera=Root.AddComponent<Camera>();Camera.transform.position=new Vector3(0,1.5f,0);var go=new GameObject("Dock");Dock=go.AddComponent<WorldActionDock>();Dock.Build(Camera,false);Dock.SetExitAvailable(true);}
  public void Tick(float now,float dt,float near=float.PositiveInfinity)=>Call(Dock,"Tick",now,dt,near);
  public void Dispose(){Object.DestroyImmediate(Dock.gameObject);Object.DestroyImmediate(Root);}
 }
 [Test]public void TurningWaitsBeforeGlidingAndArrivesAfterOneSecond()
 {
  using var f=new Fixture();f.Tick(10,.25f);var first=f.Dock.transform.position;
  f.Camera.transform.rotation=Quaternion.Euler(0,50,0);f.Tick(10.2f,.2f);Assert.That(f.Dock.transform.position,Is.EqualTo(first));Assert.That((bool)Get(f.Dock,"m_Gliding"),Is.False);
  f.Tick(10.41f,.21f);Assert.That((bool)Get(f.Dock,"m_Gliding"),Is.True);
  f.Tick(11.02f,.61f);Assert.That(Vector3.Distance(f.Dock.transform.position,WorldActionDock.TargetPosition(f.Camera.transform.position,f.Camera.transform.forward)),Is.LessThan(.0001f));
 }
 [Test]public void NearbyFingerStopsAnAlreadyMovingDockIncludingRotation()
 {
  using var f=new Fixture();f.Tick(10,.25f);f.Camera.transform.rotation=Quaternion.Euler(0,50,0);f.Tick(10.41f,.41f);f.Tick(10.6f,.19f);
  var position=f.Dock.transform.position;var rotation=f.Dock.transform.rotation;
  f.Camera.transform.rotation=Quaternion.Euler(0,80,0);f.Camera.transform.position+=Vector3.right*.4f;f.Tick(10.8f,.2f,.1f);f.Tick(13,2.2f,.1f);
  Assert.That(f.Dock.transform.position,Is.EqualTo(position));Assert.That(Quaternion.Angle(f.Dock.transform.rotation,rotation),Is.LessThan(.001f));Assert.That((bool)Get(f.Dock,"m_Gliding"),Is.False);
 }
 [Test]public void ReachDistanceIsMeasuredFromEachButtonInsteadOfDockCentre()
 {
  using var f=new Fixture();f.Tick(10,.25f);Physics.SyncTransforms();
  var point=f.Dock.transform.TransformPoint(Vector3.right*.20f);
  var distance=f.Dock.GetComponentsInChildren<WorldDockButton>().Min(b=>b.DistanceTo(point));
  Assert.That(Vector3.Distance(point,f.Dock.transform.position),Is.GreaterThan(.12f));Assert.That(distance,Is.LessThan(.12f));Assert.That(WorldActionDock.FollowHeld(distance,2),Is.True);
 }
 [Test]public void LockedRainExplainsWithoutTriggeringAndReadyRainTriggersOnce()
 {
  using var f=new Fixture();int count=0;f.Dock.ReleasePressed+=()=>count++;f.Tick(10,.25f);
  Call(f.Dock,"PressShowerAt",11f);Assert.That(count,Is.Zero);
  Assert.That(f.Dock.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Dock Toast").sprite.name,Is.EqualTo("dock_toast_locked_zh"));
  f.Dock.SetReleaseAvailable(true);Call(f.Dock,"PressShowerAt",12f);Call(f.Dock,"PressShowerAt",12.1f);Assert.That(count,Is.EqualTo(1));
 }
 [Test]public void ExitConfirmationNeedsTwoSettledPressesWithinThreeSeconds()
 {
  using var f=new Fixture();Set(f.Dock,"m_ConfirmExit",true);int exits=0;f.Dock.ExitPressed+=()=>exits++;f.Tick(10,.25f);
  Call(f.Dock,"PressExitAt",11f);Assert.That(exits,Is.Zero);
  Call(f.Dock,"PressExitAt",11.5f);Assert.That(exits,Is.EqualTo(1));
  Call(f.Dock,"PressExitAt",15f);Assert.That(exits,Is.EqualTo(1));
  Call(f.Dock,"PressExitAt",19f);Assert.That(exits,Is.EqualTo(1),"Expired confirmation starts a new window");
  Call(f.Dock,"PressExitAt",19.5f);Assert.That(exits,Is.EqualTo(2));
 }
 [Test]public void SuppressionAndRoomTransitionImmediatelyDisableBothInputPaths()
 {
  using var f=new Fixture();f.Tick(10,.25f);int count=0;f.Dock.ExitPressed+=()=>count++;f.Dock.ReleasePressed+=()=>count++;f.Dock.SetReleaseAvailable(true);
  f.Dock.Suppressed=true;Assert.That(f.Dock.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);Call(f.Dock,"PressExitAt",11f);Call(f.Dock,"PressShowerAt",11f);Assert.That(count,Is.Zero);
  f.Dock.Suppressed=false;f.Dock.SetRoomInteractionEnabled(false);Assert.That(f.Dock.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);Call(f.Dock,"PressExitAt",12f);Assert.That(count,Is.Zero);
 }
 [Test]public void ReturningAndReenablingPlaceDockAtTheNewFront()
 {
  using var f=new Fixture();f.Tick(10,.25f);f.Dock.Suppressed=true;f.Tick(11,.2f);
  f.Camera.transform.position+=Vector3.right*2;f.Camera.transform.rotation=Quaternion.Euler(0,120,0);f.Dock.Suppressed=false;f.Tick(12,.25f);
  Assert.That(Vector3.Distance(f.Dock.transform.position,WorldActionDock.TargetPosition(f.Camera.transform.position,f.Camera.transform.forward)),Is.LessThan(.0001f));
  f.Dock.gameObject.SetActive(false);Call(f.Dock,"OnDisable");f.Camera.transform.position+=Vector3.forward*2;f.Dock.gameObject.SetActive(true);f.Tick(13,.25f);
  Assert.That(Vector3.Distance(f.Dock.transform.position,WorldActionDock.TargetPosition(f.Camera.transform.position,f.Camera.transform.forward)),Is.LessThan(.0001f));
 }
}
