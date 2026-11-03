using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

var core = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var reflection = Assembly.LoadFrom(Path.GetFullPath(args[1]));
var lambdas = core.GetType("Bing.Expressions.Lambdas", true)!;
var getValue = (Func<Expression, object?>)lambdas.GetMethod("GetValue")!.CreateDelegate(typeof(Func<Expression, object?>));
var count = (Func<LambdaExpression, int>)lambdas.GetMethod("GetConditionCount")!.CreateDelegate(typeof(Func<LambdaExpression, int>));
var refs = core.GetType("Bing.Reflection.Reflections", true)!;
var pathGetter = refs.GetMethod("GetMemberValueByPath", BindingFlags.Public | BindingFlags.Static)!;
var pathSetter = refs.GetMethods().Single(m=>m.Name=="TrySetMemberValueByPath" && !m.IsGenericMethod);
var optionsType = core.GetType("Bing.Reflection.MemberPathOptions", true)!;
var closed = 7;
var holder = new Holder { Value = 9 };
Expression<Func<Model,bool>> literal = x => x.Number == 7;
Expression<Func<Model,bool>> captured = x => x.Number == closed;
Expression<Func<Model,bool>> property = x => x.Number == holder.Value;
Expression<Func<Model,bool>> computed = x => x.Number == Math.Abs(closed);
Expression<Func<Model,bool>> negatedGroup = x => !(x.Number > 0 && x.Number < 10);
var findings = new List<object>();
void Case(string name, Func<object?> run) {
 try { findings.Add(new {name,actual=run(),exception=(string?)null}); }
 catch(Exception ex) { var cause=ex is TargetInvocationException {InnerException: not null} tie ? tie.InnerException : ex; findings.Add(new {name,actual=(object?)null,exception=cause!.GetType().Name+": "+cause.Message}); }
}
if (args.Contains("--cases")) {
 Case("interface-hidden-property-public",()=>pathGetter.Invoke(null,new object?[]{new Root(),"Child.Value",null}));
 var options=Activator.CreateInstance(optionsType)!;
 optionsType.GetProperty("IncludeNonPublic")!.SetValue(options,true);
 Case("interface-hidden-property-include-nonpublic",()=>pathGetter.Invoke(null,new object?[]{new Root(),"Child.Value",options}));
 Case("interface-hidden-property-set",()=>{
  var root = new Root();
  var success=pathSetter.Invoke(null,new object?[]{root,"Child.Value",42,null});
  return new {success,expectedMember=root.Child.Value,baseMember=((ABase)root.Child).Value};
 });
 Case("count-negated-and",()=>count(negatedGroup));
 var text="alphabet";
 Expression<Func<Model,bool>> nestedContains=x=>new[]{true,false}.Contains(text.Contains("a"));
 Case("nested-contains-value",()=>getValue(nestedContains));
 Expression<Func<Model,bool>> containsQuery=x=>x.Flags.Contains(text.Contains("a"));
 Case("contains-query-computed-argument",()=>getValue(containsQuery));
 Expression<Func<Model,bool>> directBooleanComparison=x=>x.Flag==true;
 Case("bool-comparison-convention",()=>getValue(directBooleanComparison));
}
var measures=new List<object>();
if (args.Contains("--perf")) {
 measures.Add(Measure("lambda-literal",()=>getValue(literal)));
 measures.Add(Measure("lambda-captured-field",()=>getValue(captured)));
 measures.Add(Measure("lambda-captured-property",()=>getValue(property)));
 measures.Add(Measure("lambda-computed-value",()=>getValue(computed)));
 var typeVisit=reflection.GetType("Bing.Reflection.TypeVisit",true)!;
 var instance=typeVisit.GetMethod("CreateInstance",new[]{typeof(IDictionary<string,object>)})!.Invoke(null,new object[]{new Dictionary<string,object>{{"First",1},{"Second",2}}})!;
 var get=(Func<string,object>)instance.GetType().GetMethod("GetPropertyValue")!.CreateDelegate(typeof(Func<string,object>),instance);
 var set=(Action<string,object>)instance.GetType().GetMethod("SetPropertyValue")!.CreateDelegate(typeof(Action<string,object>),instance);
 measures.Add(Measure("dynamic-first-read",()=>get("First")));
 measures.Add(Measure("dynamic-second-read",()=>get("Second")));
 object boxed=9;
 measures.Add(Measure("dynamic-second-write",()=>{set("Second",boxed);return 0;}));
 var constructor=Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(instance.GetType()),typeof(object))).Compile();
 var parameter=Expression.Parameter(typeof(object));
 var getterMethod=instance.GetType().GetMethod("GetPropertyValue")!;
 var nameParameter=Expression.Parameter(typeof(string));
 var read=Expression.Lambda<Func<object,string,object>>(Expression.Call(Expression.Convert(parameter,getterMethod.DeclaringType!),getterMethod,nameParameter),parameter,nameParameter).Compile();
 measures.Add(Measure("dynamic-create-two-reads",()=>{var item=constructor();read(item,"First");return read(item,"Second");}));
 var ext=core.GetType("Bing.Reflection.PropertyInfoExtensions",true)!;
 var prop=typeof(Model).GetProperty(nameof(Model.Number))!;
 var objectFactory=(Func<PropertyInfo,Func<Model,object>>)ext.GetMethods().Single(m=>m.Name=="GetValueGetter"&&m.IsGenericMethodDefinition&&m.GetGenericArguments().Length==1).MakeGenericMethod(typeof(Model)).CreateDelegate(typeof(Func<PropertyInfo,Func<Model,object>>));
 measures.Add(Measure("object-getter-factory-warm",()=>objectFactory(prop)));
 var typedMethod=ext.GetMethods().SingleOrDefault(m=>m.Name=="GetValueGetter"&&m.IsGenericMethodDefinition&&m.GetGenericArguments().Length==2);
 if(typedMethod!=null){
  var typedFactory=(Func<PropertyInfo,Func<Model,int>>)typedMethod.MakeGenericMethod(typeof(Model),typeof(int)).CreateDelegate(typeof(Func<PropertyInfo,Func<Model,int>>));
  measures.Add(Measure("typed-getter-factory-warm",()=>typedFactory(prop)));
  var getter=typedFactory(prop);var model=new Model{Number=3};
  measures.Add(Measure("typed-getter-invoke",()=>getter(model)));
 }
}
Console.WriteLine(JsonSerializer.Serialize(new{runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,coreMvid=core.ManifestModule.ModuleVersionId,reflectionMvid=reflection.ManifestModule.ModuleVersionId,warmup=2000,iterations=10000,repetitions=5,concurrency=1,findings,measures},new JsonSerializerOptions{WriteIndented=true}));
static object Measure<T>(string name,Func<T> action){
 for(int i=0;i<2000;i++)action();
 double[] ns=new double[5],bytes=new double[5];T? result=default;
 for(int j=0;j<5;j++){
  var a=GC.GetAllocatedBytesForCurrentThread();var t=Stopwatch.GetTimestamp();
  for(int i=0;i<10000;i++)result=action();
  ns[j]=(Stopwatch.GetTimestamp()-t)*1e9/Stopwatch.Frequency/10000;
  bytes[j]=(GC.GetAllocatedBytesForCurrentThread()-a)/10000d;
 }
 GC.KeepAlive(result);Array.Sort(ns);Array.Sort(bytes);
 return new{name,nanoseconds=ns[2],bytesPerOperation=bytes[2]};
}
public class Model { public int Number{get;set;} public bool Flag{get;set;} public List<bool> Flags{get;}=new(); }
public class Holder { public int Value{get;set;} }
public interface ABase { int Value{get;set;} }
public interface ZChild:ABase { new int Value{get;set;} }
public class Both:ZChild { int ABase.Value{get;set;}=1; int ZChild.Value{get;set;}=2; }
public class Root { public ZChild Child{get;}=new Both(); }
