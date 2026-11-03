using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Text.Json;
var asm=Assembly.LoadFrom(Path.GetFullPath(args[0]));
var type=asm.GetType("Bing.Expressions.Lambdas",true)!;
var name=(Func<Expression,string>)type.GetMethod("GetName")!.CreateDelegate(typeof(Func<Expression,string>));
var value=(Func<Expression,object>)type.GetMethod("GetValue")!.CreateDelegate(typeof(Func<Expression,object>));
var member=(Func<Expression,MemberInfo>)type.GetMethod("GetMember")!.CreateDelegate(typeof(Func<Expression,MemberInfo>));
var results=new List<object>();
void Case(string label,Func<object?> run){try{results.Add(new{label,actual=run(),error=(string?)null});}catch(Exception e){results.Add(new{label,actual=(object?)null,error=e.GetType().Name});}}
Expression<Func<Model,bool>> checkedCast=x=>checked((short)x.Number)==1;
Expression<Func<Model,bool>> ordinaryCast=x=>(short)x.Number==1;
Expression<Func<Model,short>> checkedMember=x=>checked((short)x.Number);
Case("checked-name",()=>name(checkedCast));
Case("checked-member",()=>member(checkedMember)?.Name);
Case("ordinary-cast-control",()=>name(ordinaryCast));
int[] ids={1,2};
Expression<Func<Model,bool>> parallel=x=>ids.AsParallel().Contains(x.Number);
Expression<Func<Model,bool>> parallelComparer=x=>ids.AsParallel().Contains(x.Number,EqualityComparer<int>.Default);
Expression<Func<Model,bool>> enumerable=x=>ids.Contains(x.Number);
Case("parallel-method",()=>((MethodCallExpression)parallel.Body).Method.DeclaringType!.FullName);
Case("parallel-value",()=>string.Join(",",(IEnumerable<int>)value(parallel)));
Case("parallel-comparer-value",()=>string.Join(",",(IEnumerable<int>)value(parallelComparer)));
Case("enumerable-control",()=>string.Join(",",(IEnumerable<int>)value(enumerable)));
var captured=-7;
Expression Create(){Expression<Func<Model,bool>> e=x=>x.Number==Math.Abs(captured);return e;}
for(int i=0;i<2000;i++)value(Create());
const int iterations=10000;double[] ns=new double[5],bytes=new double[5];
for(int sample=0;sample<5;sample++){
 var start=GC.GetAllocatedBytesForCurrentThread();var tick=Stopwatch.GetTimestamp();
 for(int i=0;i<iterations;i++)value(Create());
 ns[sample]=(Stopwatch.GetTimestamp()-tick)*1e9/Stopwatch.Frequency/iterations;
 bytes[sample]=(GC.GetAllocatedBytesForCurrentThread()-start)/ (double)iterations;
}
Array.Sort(ns);Array.Sort(bytes);
Console.WriteLine(JsonSerializer.Serialize(new{runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,coreMvid=asm.ManifestModule.ModuleVersionId,results,cold=new{scenario="create-expression-and-evaluate-once",iterations,repetitions=5,warmup=2000,concurrency=1,nanoseconds=ns[2],bytesPerOperation=bytes[2]}},new JsonSerializerOptions{WriteIndented=true}));
public class Model{public int Number{get;set;}}
