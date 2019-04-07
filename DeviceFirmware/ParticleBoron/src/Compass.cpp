#include "Compass.h"

/* Assign a unique ID to this sensor at the same time */
QMC5883L compass;

Compass::Compass()
{
    compass = QMC5883L();
}

bool Compass::begin()
{
    // Check that a device responds at the compass address - don't continue if it doesn't -
    Wire.beginTransmission(QMC5883L_Address);
    int error = Wire.endTransmission();

    if (!error)
    {
        // configure the control registers using static settings above
        // compass autoranges, but starts in the mode given
        compass.dataRegister.OSR_RNG_ODR_MODE = (OSR << 6) | (RNG << 4) | (ODR << 2) | MODE;
        compass.dataRegister.CR2_INT_ENABLE = CR2;
        compass.dataRegister.SET_RESET_PERIOD = RESETPERIOD;

        Serial.println("Configuring QMC5883L - OSR 512, range +/-2 Gauss, ODR 10, Continuous");
        error = compass.Configure(compass.dataRegister); // use static settings from above - can access register data directly if required..
        if (error != 0) Serial.println(compass.GetErrorText(error));
    }
    return !error;
}

CompassReading Compass::getReading()
{
    CompassReading result;

    MagnetometerScaled scaled = compass.ReadScaledAxis(&compass.dataRegister);

    result.x = scaled.XAxis;
    result.y = scaled.YAxis;
    result.z = scaled.ZAxis;

    return result;
}