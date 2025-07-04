#include <ArduinoBLE.h>

BLEService bleService("f9c97383-3025-4a1f-9903-8b313565184e");
BLEStringCharacteristic bleCharacteristic("67391cf3-acc4-4a8c-af95-0131221895f2", BLERead | BLENotify, 96);

int inputPins[] = {
  2, 4,
  5, 16, 17, 26,
  27, 12, 18, 19,
  21, 22, 23, 25
};

bool hasInput = false;

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
}

void loop() {
    BLEDevice central = BLE.central();
    if (central) {
      Serial.println("BT: connected to central: ");
      Serial.println(central.address());

      while (central.connected()) {
          bleCharacteristic.writeValue(getPinState()); 
          delay(10);
      }

      Serial.println("BT: disconnected from central: ");
      Serial.println(central.address()); 
    }
    else {
      Serial.println(getPinState());
      delay(10);
    }
}
