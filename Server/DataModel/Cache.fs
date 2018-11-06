namespace WeatherStation

module Cache =
    
    open System

    type private CacheOpperation<'TKey, 'TValue> = 'TKey * Async<'TValue> * AsyncReplyChannel<'TValue>

    let private keyValueCache<'TKey, 'TValue when 'TKey : comparison> =
        let rec processor settings (mailBox : MailboxProcessor<CacheOpperation<'TKey, 'TValue>>)=
            async {
                let! key, valueBuilder, reply = mailBox.Receive()
                let cachedValue = settings |> Map.tryFind key
                let! nextSettings, value =
                    if cachedValue.IsSome
                    then async { return settings, cachedValue.Value }
                    else
                        async {
                            let! value = valueBuilder                        
                            let nextSettings = settings |> Map.add key value
                            return nextSettings, value
                        }
                let hitOrMiss = if cachedValue.IsSome then "HIT" else "MISS"
                printfn "Cache Lookup [%A] -> %s {%A}" key hitOrMiss value
                reply.Reply(value)
                do! processor nextSettings mailBox                        
            }
        MailboxProcessor<CacheOpperation<'TKey, 'TValue>>.Start (processor Map.empty)

    type Cache<'TKey, 'TValue when 'TKey : comparison>() =
        let cache = keyValueCache<'TKey, 'TValue>
        member this.GetOrCreate(key, defaultValueBuilder) =
            async {
                let! value = cache.PostAndAsyncReply( fun replyChannel -> key, defaultValueBuilder, replyChannel)
                return value
            }

    let buildTimedCache<'TValue> load expiration refresh build = 
    
        let rec getNextValue currentDate cachedValue =
            match cachedValue with
            | Some value ->                        
                if expiration value > currentDate
                then async { return value }
                else
                    printfn "Value expired on %A" (expiration value)
                    refresh value
            | None -> 
                async {
                    let! (possibleValue : 'TValue option) = load
                    return!
                        if possibleValue.IsSome 
                        then getNextValue currentDate possibleValue
                        else
                            async {
                                let! builtValue = build
                                return! getNextValue currentDate (Some builtValue) } }

        let rec cache cachedValue (mailBox : MailboxProcessor<DateTime * AsyncReplyChannel<Result<'TValue, exn>>>) =
            async {
                let! (currentDate, responseChannel) = mailBox.Receive()                
                let! value = 
                    getNextValue currentDate cachedValue
                    |> Async.Catch
                
                let nextValue, response =
                    match value with
                    | Choice1Of2 value -> Some value, Ok value
                    | Choice2Of2 exn -> None, Error exn
                responseChannel.Reply response
                do! cache nextValue mailBox}

        let mailBox = MailboxProcessor.Start (cache None)

        fun currentDate -> mailBox.PostAndAsyncReply (fun channel -> currentDate, channel)

