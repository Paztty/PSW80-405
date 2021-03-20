

#define SS_PIN 2
#define ON LOW
#define OFF HIGH

const String POWER_ON     =       ":POWER_ON";
const String POWER_OFF    =       ":POWER_OFF";
const String POWER_DELAY_ON   =   "OUTPut:DELay:ON 0.5";
const String POWER_DELAY_OFF  =   "OUTPut:DELay:OFF 0.5";

unsigned char LAST_SS_STATE = OFF;


void setup()
{
    pinMode(SS_PIN, INPUT_PULLUP);

    LAST_SS_STATE = !digitalRead(SS_PIN);

    Serial.begin(9600);
    Serial.println(POWER_DELAY_ON);
    Serial.println(POWER_DELAY_OFF);
}

void loop()
{
    if (LAST_SS_STATE == OFF)
    {
        if (digitalRead(SS_PIN) == ON)
        {
            while (digitalRead(SS_PIN) == ON)
            {
                delay(500);
            }
            LAST_SS_STATE = ON;
            Serial.println(POWER_ON);
        }
    }
    else
    {
        if (digitalRead(SS_PIN) == OFF)
        {
            while (digitalRead(SS_PIN) == OFF)
            {
                delay(500);
            }
            LAST_SS_STATE = OFF;
            Serial.println(POWER_OFF);
        }
    }
    

}



