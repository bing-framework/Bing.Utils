using Bing.Captcha;
using Bing.Drawing;
using Xunit;

namespace Bing.Utils.Captcha.Tests;

public class CaptchaTests
{
    private static readonly CaptchaContext Context = new("login", "trusted-session", "tab-1");
    private static readonly CaptchaKey Key = new("v1", Enumerable.Repeat((byte)7, 32).ToArray());
    private sealed class Clock : ICaptchaClock { public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1000); }
    private sealed class Renderer : ICaptchaRenderer
    {
        public string Answer = "";
        public bool Fail;
        public CaptchaImage Render(string text, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException();
            Answer = text;
            return new CaptchaImage(new byte[] {137,80,78,71,13,10,26,10}, 160, 60);
        }
    }
    private static StaticCaptchaKeyProvider Keys(params CaptchaKey[] keys) => new(keys.Last().Version, keys);
    private static CaptchaService Service(ICaptchaStore store, Renderer renderer, ICaptchaClock clock, CaptchaServiceOptions? options = null, ICaptchaKeyProvider? keys = null) => new(store, renderer, keys ?? Keys(Key), options, clock);

    [Theory]
    [InlineData(CaptchaType.Number)] [InlineData(CaptchaType.NumberAndLetter)] [InlineData(CaptchaType.ChineseChar)]
    public async Task Create_AllCharacterTypes_CanBeConsumedOnce(CaptchaType type)
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock: clock); var renderer = new Renderer();
        var service = Service(store, renderer, clock, new CaptchaServiceOptions { Type = type });
        var challenge = await service.CreateAsync(Context);
        Assert.Equal(22, challenge.CaptchaId.Length);
        Assert.Equal(clock.UtcNow.AddSeconds(120), challenge.ExpiresAt);
        Assert.Equal(5, renderer.Answer.Length);
        Assert.All(renderer.Answer, c => Assert.Contains(c, CaptchaAlphabet.GetCharacters(type)));
        Assert.True((await service.VerifyAsync(Context, challenge.CaptchaId, renderer.Answer)).Succeeded);
        Assert.False((await service.VerifyAsync(Context, challenge.CaptchaId, renderer.Answer)).Succeeded);
    }
    [Fact]
    public async Task Verify_WrongAnswer_ConsumesAndConcurrentSuccessIsAtMostOne()
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock: clock); var r = new Renderer(); var s = Service(store,r,clock);
        var c = await s.CreateAsync(Context);
        Assert.Equal(CaptchaVerifyStatus.Mismatch, (await s.VerifyAsync(Context,c.CaptchaId,"WRONG-ANSWER")).Status);
        Assert.False((await s.VerifyAsync(Context,c.CaptchaId,r.Answer)).Succeeded);
        c = await s.CreateAsync(Context); var answer = r.Answer;
        var results = await Task.WhenAll(Enumerable.Range(0,32).Select(_ => Task.Run(() => s.VerifyAsync(Context,c.CaptchaId,answer))));
        Assert.Equal(1, results.Count(x => x.Succeeded));
    }
    [Theory] [InlineData(-1, true)] [InlineData(0, false)] [InlineData(1, false)]
    public async Task Verify_ExpiryBoundary_UsesGreaterThanOrEqual(int milliseconds, bool expected)
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock:clock); var r = new Renderer(); var s = Service(store,r,clock);
        var c = await s.CreateAsync(Context); clock.UtcNow = c.ExpiresAt.AddMilliseconds(milliseconds);
        Assert.Equal(expected,(await s.VerifyAsync(Context,c.CaptchaId,r.Answer)).Succeeded);
    }
    [Fact]
    public async Task Refresh_OldIdAndOtherBindings_DoNotDeleteCurrent()
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock:clock); var r = new Renderer(); var s = Service(store,r,clock);
        var old = await s.CreateAsync(Context); var current = await s.CreateAsync(Context);
        foreach (var context in new[] {new CaptchaContext("register","trusted-session","tab-1"), new CaptchaContext("login","other","tab-1"),new CaptchaContext("login","trusted-session","other")})
            Assert.False((await s.VerifyAsync(context,current.CaptchaId,r.Answer)).Succeeded);
        Assert.False((await s.VerifyAsync(Context,old.CaptchaId,r.Answer)).Succeeded);
        Assert.True((await s.VerifyAsync(Context,current.CaptchaId,r.Answer)).Succeeded);
    }
    [Fact]
    public async Task Rotation_UsesStoredPolicyAndRetainedKey()
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock:clock); var r = new Renderer();
        var s = Service(store,r,clock); var c = await s.CreateAsync(Context);
        var rotated = Service(store,r,clock,new CaptchaServiceOptions { IgnoreAsciiCase = false },Keys(Key,new CaptchaKey("v2",new byte[32])));
        var fullWidth = new string(r.Answer.ToLowerInvariant().Select(ch => ch >= '!' && ch <= '~' ? (char)(ch + 0xfee0) : ch).ToArray());
        Assert.True((await rotated.VerifyAsync(Context,c.CaptchaId,"  "+fullWidth+"  ")).Succeeded);
    }
    [Fact]
    public async Task InvalidInputAndRenderingFailure_LeaveCurrentChallenge()
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock:clock); var r = new Renderer(); var s = Service(store,r,clock);
        var c = await s.CreateAsync(Context); var answer = r.Answer;
        await Assert.ThrowsAsync<ArgumentException>(() => s.VerifyAsync(Context,c.CaptchaId,new string('a',65)));
        r.Fail = true; await Assert.ThrowsAsync<CaptchaRendererException>(() => s.CreateAsync(Context));
        Assert.True((await s.VerifyAsync(Context,c.CaptchaId,answer)).Succeeded);
    }
    [Fact]
    public async Task Memory_CapacityCancellationAndDisposal_AreBounded()
    {
        var clock = new Clock(); var store = new MemoryCaptchaStore(new MemoryCaptchaStoreOptions {Capacity=1},clock);
        var entry = new CaptchaEntry("id",new byte[32],"v1",clock.UtcNow.AddMinutes(1),true);
        await store.ReplaceAsync(Context,entry);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.TryTakeAsync(Context,"id",new CancellationToken(true)));
        var other = new CaptchaContext("login","other","flow"); await store.ReplaceAsync(other,entry);
        Assert.Null(await store.TryTakeAsync(Context,"id")); Assert.Equal(1,store.Count);
        store.Dispose(); store.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => store.TryTakeAsync(other,"id"));
    }
    [Fact]
    public async Task OptionsAndBuffers_AreSnapshots()
    {
        var clock = new Clock(); using var store = new MemoryCaptchaStore(clock:clock); var r = new Renderer(); var options = new CaptchaServiceOptions();
        var s = Service(store,r,clock,options); options.Length=8; options.AllowedPurposes[0]="other";
        var c = await s.CreateAsync(Context); Assert.Equal(5,r.Answer.Length);
        c.Image.Bytes[0]=0; Assert.Equal(137,c.Image.Bytes[0]);
        var bytes = new byte[32]; var key = new CaptchaKey("copy",bytes); bytes[0]=1; Assert.Equal(0,key.GetBytes()[0]);
        Assert.NotEqual(new CaptchaContext("ab","c","d").GetStorageKey(),new CaptchaContext("a","bc","d").GetStorageKey());
    }
    private sealed class BrokenStore : ICaptchaStore
    {
        public Task ReplaceAsync(CaptchaContext c,CaptchaEntry e,CancellationToken t=default) => throw new IOException("offline");
        public Task<CaptchaEntry?> TryTakeAsync(CaptchaContext c,string id,CancellationToken t=default) => throw new IOException("offline");
    }
    [Fact]
    public async Task StoreFaults_AreNotOrdinaryMismatch()
    {
        var s=Service(new BrokenStore(),new Renderer(),new Clock());
        await Assert.ThrowsAsync<CaptchaStoreException>(()=>s.CreateAsync(Context));
        await Assert.ThrowsAsync<CaptchaStoreException>(()=>s.VerifyAsync(Context,"id","answer"));
    }
    // 此存储故意忽略上下文，验证保护来自摘要本身，而非仅依赖键查找。
    private sealed class RecordStore : ICaptchaStore
    {
        public CaptchaEntry? Entry;
        public Task ReplaceAsync(CaptchaContext c,CaptchaEntry e,CancellationToken t=default) { Entry=e;return Task.CompletedTask; }
        public Task<CaptchaEntry?> TryTakeAsync(CaptchaContext c,string id,CancellationToken t=default) { var e=Entry;Entry=null;return Task.FromResult(e); }
    }
    [Theory]
    [InlineData("purpose")] [InlineData("binding")] [InlineData("flow")]
    [InlineData("id")] [InlineData("expiry")] [InlineData("case")] [InlineData("key")]
    public async Task Authentication_RejectsTamperedMetadata(string field)
    {
        var clock=new Clock();var store=new RecordStore();var r=new Renderer();
        var keys=new StaticCaptchaKeyProvider("v1",new[]{Key,new CaptchaKey("v2",Key.GetBytes())});
        var service=Service(store,r,clock,keys:keys);var c=await service.CreateAsync(Context);var e=store.Entry!;
        var context=new CaptchaContext(field=="purpose"?"register":Context.Purpose,field=="binding"?"other":Context.BindingId,field=="flow"?"other":Context.FlowId);
        var id=field=="id"?"different-id":c.CaptchaId;
        store.Entry=new CaptchaEntry(id,e.AnswerHash,field=="key"?"v2":e.KeyVersion,field=="expiry"?e.ExpiresAt.AddSeconds(1):e.ExpiresAt,field=="case"?false:e.IgnoreAsciiCase);
        Assert.Equal(CaptchaVerifyStatus.Mismatch,(await service.VerifyAsync(context,id,r.Answer)).Status);
    }
    [Fact]
    public async Task RemovedKey_IsInvalid_AndOutputLimitPreservesPreviousRecord()
    {
        var clock=new Clock();using var store=new MemoryCaptchaStore(clock:clock);var r=new Renderer();
        var service=Service(store,r,clock);var c=await service.CreateAsync(Context);var answer=r.Answer;
        var limited=Service(store,r,clock,new CaptchaServiceOptions{MaxImageBytes=1});
        await Assert.ThrowsAsync<CaptchaRendererException>(()=>limited.CreateAsync(Context));
        Assert.True((await service.VerifyAsync(Context,c.CaptchaId,answer)).Succeeded);
        c=await service.CreateAsync(Context);
        var noOldKey=Service(store,r,clock,keys:Keys(new CaptchaKey("v2",new byte[32])));
        Assert.Equal(CaptchaVerifyStatus.InvalidOrExpired,(await noOldKey.VerifyAsync(Context,c.CaptchaId,r.Answer)).Status);
    }
    [Theory]
    [InlineData(3,120)] [InlineData(9,120)] [InlineData(5,29)] [InlineData(5,301)]
    public void Configuration_RejectsOutOfRange(int length,int lifetimeSeconds)
    {
        using var store=new MemoryCaptchaStore();
        Assert.Throws<ArgumentOutOfRangeException>(()=>Service(store,new Renderer(),new Clock(),new CaptchaServiceOptions{Length=length,Lifetime=TimeSpan.FromSeconds(lifetimeSeconds)}));
    }
    [Fact]
    public async Task Memory_RefreshRacesCannotConsumeNewId_AndTimerRemovesExpired()
    {
        var clock=new Clock();using var store=new MemoryCaptchaStore(new MemoryCaptchaStoreOptions{Capacity=2,CleanupBatchSize=1,CleanupInterval=TimeSpan.FromMilliseconds(10)},clock);
        var old=new CaptchaEntry("old",new byte[32],"v1",clock.UtcNow.AddMinutes(1),true);
        var current=new CaptchaEntry("new",new byte[32],"v1",clock.UtcNow.AddMinutes(1),true);
        for(var i=0;i<32;i++)
        {
            await store.ReplaceAsync(Context,old);
            await Task.WhenAll(Task.Run(()=>store.TryTakeAsync(Context,"old")),Task.Run(()=>store.ReplaceAsync(Context,current)));
            Assert.NotNull(await store.TryTakeAsync(Context,"new"));
        }
        await store.ReplaceAsync(Context,old);clock.UtcNow=old.ExpiresAt;
        var deadline=System.Diagnostics.Stopwatch.StartNew();
        while(store.Count!=0 && deadline.Elapsed<TimeSpan.FromSeconds(2)) await Task.Delay(10);
        Assert.Equal(0,store.Count);
    }
}
