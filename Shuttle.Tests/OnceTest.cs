using JetBrains.Annotations;
using Shuttle.Core;

namespace Shuttle.Tests;

[TestSubject(typeof(Once))]
public class OnceTest {

    [Fact]
    public void HasRun_InitiallyFalse() {
        var once = new Once();
        Assert.False(once.HasRun);
    }

    [Fact]
    public void HasRun_TrueAfterRun() {
        var once = new Once();
        once.Run(() => { });
        Assert.True(once.HasRun);
    }

    [Fact]
    public void Run_ExecutesActionOnce() {
        var once = new Once();
        var count = 0;
        
        once.Run(() => count++);
        
        Assert.Equal(1, count);
    }

    [Fact]
    public void Run_DoesNotExecuteActionAgain_WhenCalledMultipleTimes() {
        var once = new Once();
        var count = 0;
        
        once.Run(() => count++);
        once.Run(() => count++);
        once.Run(() => count++);
        
        Assert.Equal(1, count);
    }

    [Fact]
    public void Run_ThrowsException_WhenCalledTwiceWithThrowIfAlreadyRunTrue() {
        var once = new Once(throwIfAlreadyRun: true);
        once.Run(() => { });
        
        var exception = Assert.Throws<InvalidOperationException>(() => once.Run(() => { }));
        Assert.Contains("This code has already been run", exception.Message);
        Assert.Contains("First call stack", exception.Message);
    }

    [Fact]
    public void ThrowIfAlreadyRun_ReflectsConstructorParameter() {
        var onceDefault = new Once();
        var onceTrue = new Once(throwIfAlreadyRun: true);
        var onceFalse = new Once(throwIfAlreadyRun: false);
        
        Assert.False(onceDefault.ThrowIfAlreadyRun);
        Assert.True(onceTrue.ThrowIfAlreadyRun);
        Assert.False(onceFalse.ThrowIfAlreadyRun);
    }

    [Fact]
    public void Run_IsThreadSafe_ExecutesOnlyOnce() {
        var once = new Once();
        var count = 0;
        var threads = new Thread[10];
        var barrier = new Barrier(threads.Length);
        
        for (int i = 0; i < threads.Length; i++) {
            threads[i] = new Thread(() => {
                barrier.SignalAndWait(); // Ensure all threads start simultaneously
                once.Run(() => Interlocked.Increment(ref count));
            });
        }
        
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();
        
        Assert.Equal(1, count);
        Assert.True(once.HasRun);
    }

    [Fact]
    public void Run_WithThrowIfAlreadyRun_IsThreadSafe() {
        var once = new Once(throwIfAlreadyRun: true);
        var count = 0;
        var exceptionCount = 0;
        var threads = new Thread[10];
        var barrier = new Barrier(threads.Length);
        
        for (int i = 0; i < threads.Length; i++) {
            threads[i] = new Thread(() => {
                barrier.SignalAndWait();
                try {
                    once.Run(() => Interlocked.Increment(ref count));
                } catch (InvalidOperationException) {
                    Interlocked.Increment(ref exceptionCount);
                }
            });
        }
        
        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();
        
        Assert.Equal(1, count);
        Assert.Equal(threads.Length - 1, exceptionCount);
    }

    [Fact]
    public void Run_WithNullAction_SetsHasRunTrue() {
        var once = new Once();
        
        once.Run(null);
        
        Assert.True(once.HasRun);
    }

    [Fact]
    public void Run_WithNullAction_DoesNotThrow() {
        var once = new Once();
        
        var exception = Record.Exception(() => once.Run(null));
        
        Assert.Null(exception);
    }

    [Fact]
    public void Run_WithNoArguments_SetsHasRunTrue() {
        var once = new Once();
        
        once.Run();
        
        Assert.True(once.HasRun);
    }

    [Fact]
    public void Run_WithNullAction_PreventsSubsequentActions() {
        var once = new Once();
        var count = 0;
        
        once.Run(null);
        once.Run(() => count++);
        
        Assert.Equal(0, count);
    }

    [Fact]
    public void Run_WithNullAction_ThrowsOnSecondCall_WhenThrowIfAlreadyRunTrue() {
        var once = new Once(throwIfAlreadyRun: true);
        
        once.Run(null);
        
        Assert.Throws<InvalidOperationException>(() => once.Run(null));
    }
}
