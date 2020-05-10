namespace WeatherStation.Tests.Server

open NUnit.Framework

[<TestFixture>]
module CacheTests =
    
    open System
    open System.Threading.Tasks

    let cacheTest currentDate initialValue buildValue refreshValue expectedValue expectLoad expectBuild expectRefresh =
        async {
            let loaded = ref false
            let built = ref false
            let refreshed = ref false

            let build = async { 
                built := true
                return buildValue }

            let loadToken = async {
                loaded := true
                return initialValue
            }
            
            let refresh _ = async {
                refreshed := true
                return refreshValue
            }
                    
            let cache = WeatherStation.Cache.buildTimedCache loadToken id refresh build
            let! value = cache currentDate

            Assert.That(value, Is.EqualTo(expectedValue : Result<DateTime, exn>), "Unexpected value")
            Assert.That(!loaded, Is.EqualTo(expectLoad : bool), "Token should have been loaded")
            Assert.That(!built, Is.EqualTo(expectBuild : bool), "Should not have built a token")
            Assert.That(!refreshed, Is.EqualTo(expectRefresh : bool), "Should not have been refreshed")
        }
        |> Async.StartAsTask
        :> Task
    
    let now() = DateTime(2018, 6, 1)
    let before() = now().AddDays(-2.0)
    let later() = now().AddDays(2.0)
        
    [<Test>]        
    let LoadInitialValue() =
        let now = now()
        let later = later()
        cacheTest now (Some later) now now (Ok later) true false false
    
    [<Test>]        
    let LoadExpiredValue() =
        let now = now()
        let before = before()
        let later = later()
        cacheTest now (Some before) now later (Ok later) true false true
    
    [<Test>]        
    let BuildInitialValue() =
        let now = now()
        let later = later()
        cacheTest now None later now (Ok later) true true false
    
    [<Test>]        
    let BuildExpiredValue() =
        let now = now()
        let before = before()
        let later = later()
        cacheTest now None before later (Ok later) true true true    
        
    [<Test>]        
    let Refresh() =
        let now = now()
        let before = before()
        let later = later()
        cacheTest now (Some before) before later (Ok later) true false true