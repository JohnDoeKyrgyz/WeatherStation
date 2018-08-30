namespace WeatherStation

module Cache =

    type private CacheOpperation<'TKey, 'TValue> = 'TKey * Async<'TValue> * AsyncReplyChannel<'TValue>

    let private cache<'TKey, 'TValue when 'TKey : comparison> =
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
        let cache = cache<'TKey, 'TValue>
        member this.GetOrCreate(key, defaultValueBuilder) =
            async {
                let! value = cache.PostAndAsyncReply( fun replyChannel -> key, defaultValueBuilder, replyChannel)
                return value
            }

