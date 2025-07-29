#include <ArduinoBLE.h>

BLEService bleService("f9c97383-3025-4a1f-9903-8b313565184e");
BLEStringCharacteristic bleCharacteristic("67391cf3-acc4-4a8c-af95-0131221895f2", BLERead | BLENotify, 96);

int inputPins[] = {
  3, 1, 22, 23,
  5, 18, 19, 21,
  2, 4, 16, 17,
  12, 27, 26, 25
};

bool hasInput = false;
unsigned long lastActivityTime = millis();
const unsigned long idleTimeout = 15 * 60 * 1000;

const unsigned long pulseDuration = 100; 
const unsigned long pulseInterval = 30000;   
unsigned long previousMillis = 0;
bool ledOn = false;

void setupPins() {
  Serial.println("Setting pins");

  for (int i = 0; i < sizeof(inputPins) / sizeof(inputPins[0]); i++) 
  {
    pinMode(inputPins[i], INPUT_PULLUP);
  }
}

String getPinState() {
  String state = "State: ";

  hasInput = false;
  for (int i = 0; i < sizeof(inputPins) / sizeof(inputPins[0]); i++) 
  {
    if(i != 0) {
      state += ",";
    }
    int value = 1 - digitalRead(inputPins[i]);
    if(value > 0) {
      hasInput = true;
    }
    state += value;
  }

  return state;
}

void enterDeepSleep() {
  uint64_t pinMask = 0;
  for (int i = 0; i < sizeof(inputPins) / sizeof(inputPins[0]); i++) {
    pinMask |= 1ULL << inputPins[i];
  }

  digitalWrite(33, LOW);
  esp_sleep_enable_ext0_wakeup(GPIO_NUM_25, LOW);
  esp_deep_sleep_start();
}

void updateStatusLED() {
  int currentMillis = millis();
  if (!ledOn && currentMillis - previousMillis >= pulseInterval) {
    digitalWrite(33, HIGH);
    ledOn = true;
    previousMillis = currentMillis;
  } else if (ledOn && currentMillis - previousMillis >= pulseDuration) {
    digitalWrite(33, LOW);
    ledOn = false;
    previousMillis = currentMillis;
  }
}

void updateStatus() {
  if (hasInput) {
    lastActivityTime = millis();
    ledOn = false;
    previousMillis = 0;
  }
  updateStatusLED();

  if (millis() - lastActivityTime > idleTimeout) {
    enterDeepSleep();
  }
}

void setup() {  
  Serial.begin(115200);

  setupPins();

  if (!BLE.begin()) {
    Serial.println("BT: failed to initialize BLE!");
  }

  BLE.setLocalName("Swipe Pad v.1.0");
  BLE.setAdvertisedService(bleService);
  bleService.addCharacteristic(bleCharacteristic);
  BLE.addService(bleService);
  bleCharacteristic.writeValue(getPinState());

  BLE.advertise();

  Serial.println("BT: advertising");

  pinMode(33, OUTPUT);
}

void loop() {
    BLEDevice central = BLE.central();
    if (central) {
      Serial.println("BT: connected to central: ");
      Serial.println(central.address());

      while (central.connected()) {
          bleCharacteristic.writeValue(getPinState());         
          updateStatus();
          delay(10);
      }

      Serial.println("BT: disconnected from central: ");
      Serial.println(central.address()); 
    }
    else {
      Serial.println(getPinState());
      updateStatus();

      delay(10);
    }
}
