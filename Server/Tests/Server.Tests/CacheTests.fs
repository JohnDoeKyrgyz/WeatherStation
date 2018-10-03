namespace WeatherStation.Tests.Server
module CacheTests =
    open System
    open Expecto

    let cacheTest currentDate initialValue buildValue refreshValue expectedValue expectLoad expectBuild expectRefresh = async {
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

        Expect.equal value expectedValue "Unexpected value"
        Expect.equal !loaded expectLoad "Token should have been loaded"
        Expect.equal !built expectBuild "Should not have built a token"
        Expect.equal !refreshed expectRefresh "Should not have been refreshed"
    }

    [<Tests>]
    let tests =
        let now = new DateTime(2018, 6, 1)
        let before = now.AddDays(-2.0)
        let later = now.AddDays(2.0)
        let cacheTest = cacheTest now
        
        testList "Timed Cache Tests" [
            testCaseAsync "Load initial value" (cacheTest (Some later) now now later true false false)
            testCaseAsync "Load expired value" (cacheTest (Some before) now later later true false true)
            testCaseAsync "Build initial value" (cacheTest None later now later true true false)
            testCaseAsync "Build expired value" (cacheTest None before later later true true true)
            testCaseAsync "Refresh" (cacheTest (Some before) before later later true false true)]