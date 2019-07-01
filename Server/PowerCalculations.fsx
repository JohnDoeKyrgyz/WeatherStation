[<Measure>]
type Amps

[<Measure>]
type Milliamps

[<Measure>]
type Volts

[<Measure>]
type Seconds

[<Measure>]
type Hours

[<Measure>]
type Watts

let sleepCurrent = 15.0<Milliamps>
let transmitCurrent = 110.0<Amps>
let readingDuration = 10<Seconds>
let systemVolts = 3.3<Volts>
let secondsPerHour = 3600.0<Seconds/Hours>

let expectedBatteryLife 
    (batteryCapacity : float<Hours Milliamps>) 
    (sleepCurent : float<Milliamps>) 
    (wakeCurrent : float<Milliamps>)
    (sleepDuration : float<Seconds>)
    (wakeDuration : float<Seconds>) =
    
    let wakeCurrentHours = (wakeCurrent * wakeDuration) / secondsPerHour
    let sleepCurentHours = (sleepCurent * sleepDuration) / secondsPerHour
    let cycleCurrent = wakeCurrentHours + sleepCurentHours

    let trueBatteryCapacity = batteryCapacity * 0.8
    let cycles = trueBatteryCapacity / cycleCurrent

    cycles * (sleepDuration + wakeDuration) / secondsPerHour

expectedBatteryLife 
    9800.0<Hours Milliamps>
    15.0<Milliamps>
    120.0<Milliamps>
    60.0<Seconds>
    10.0<Seconds>
