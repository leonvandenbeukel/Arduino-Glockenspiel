/*
 * MIT License
 * 
 * Copyright (c) 2019 Leon van den Beukel
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * Source: 
 * https://github.com/leonvandenbeukel/Arduino-Glockenspiel
 * 
 */
 
#include <Servo.h>
#include <SoftwareSerial.h>
#include <EEPROM.h>

Servo servos[8];
byte servoPins[] = {2, 3, 4, 5, 6, 7, 8, 9};
float bpm = 100;
float bpm2ms;
unsigned long lastReading;

//                    C   D   E   F   G   A   B   C2
byte hammerHit[]  = { 10, 10, 4, 10,  8,  10, 50, 116 };
byte hammerRest[] = { 44, 50,46, 46, 40,  44, 92, 150 };  

#define sR  0
#define sC  1
#define sD  2
#define sE  4
#define sF  8
#define sG  16
#define sA  32
#define sB  64
#define sC2 128

#define delayBetween 100
#define maxMelodySize 512
int melody[maxMelodySize];
int currentNoteCount = 0;

SoftwareSerial BTSerial(11, 12); // RX, TX
String btBuffer;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  Serial.begin(115200);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  // Set beats per minute to ms
  bpm2ms = (60.0 / bpm) * 1000;

  // Read calibration values from memory
  for (int i=0; i<8; i++) {
    byte calibValueHit  = EEPROM.read(i); 
    byte calibValueRest = EEPROM.read(i+8); 
    Serial.print("Calib values (");
    Serial.print(i);
    Serial.print("): ");
    Serial.print(calibValueHit);    
    Serial.print(",");
    Serial.println(calibValueRest);    

    // Store values if not set
    if (calibValueHit == 255 && calibValueRest == 255) {
      EEPROM.write(i, hammerHit[i]);
      EEPROM.write(i + 8, hammerRest[i]);
    }    
  }

  for (byte i=0; i<sizeof(servoPins); i++){
    servos[i].attach(servoPins[i]);
    servos[i].write(hammerRest[i]);    
  }
  
  delay(delayBetween);
  BTSerial.begin(9600);
}

void loop() {

  if (millis() - lastReading >= bpm2ms) {
    
    lastReading = millis();
    digitalWrite(LED_BUILTIN, HIGH);

    int val = melody[currentNoteCount++];

    Serial.print("Current note count: ");
    Serial.print(currentNoteCount);
    Serial.print(" Notes value: ");    
    Serial.print(val);    
    Serial.println();

    hammerDown(val);
    
    if (melody[currentNoteCount] == -1) 
      currentNoteCount = 0;

    digitalWrite(LED_BUILTIN, LOW);
  }

  if (BTSerial.available())
  {
    char received = BTSerial.read();
    btBuffer += received; 

    if (received == '|')
    {
        processCommand();
        btBuffer = "";
    }
  }  
}

void hammerDown(int value) {

  if ((value & sC) == sC) servos[0].write(hammerHit[0]);    // C
  if ((value & sD) == sD) servos[1].write(hammerHit[1]);    // D
  if ((value & sE) == sE) servos[2].write(hammerHit[2]);    // E
  if ((value & sF) == sF) servos[3].write(hammerHit[3]);    // F
  if ((value & sG) == sG) servos[4].write(hammerHit[4]);    // G
  if ((value & sA) == sA) servos[5].write(hammerHit[5]);    // A
  if ((value & sB) == sB) servos[6].write(hammerHit[6]);    // B
  if ((value & sC2) == sC2) servos[7].write(hammerHit[7]);  // C2

  delay(delayBetween);

  if ((value & sC) == sC) servos[0].write(hammerRest[0]);   // C
  if ((value & sD) == sD) servos[1].write(hammerRest[1]);   // D
  if ((value & sE) == sE) servos[2].write(hammerRest[2]);   // E
  if ((value & sF) == sF) servos[3].write(hammerRest[3]);   // F
  if ((value & sG) == sG) servos[4].write(hammerRest[4]);   // G
  if ((value & sA) == sA) servos[5].write(hammerRest[5]);   // A
  if ((value & sB) == sB) servos[6].write(hammerRest[6]);   // B
  if ((value & sC2) == sC2) servos[7].write(hammerRest[7]); // C2
}

void processCommand() {
  int nrOfHits = 0;
  char separator = ',';

  // Reset melody
  for (int i=0; i<maxMelodySize; i++) 
    melody[i] = 0;  

  Serial.print("BT received, raw data: ");
  Serial.println(btBuffer);

  // Count nr of notes
  for (int i=0; i<btBuffer.length(); i++) 
    if (btBuffer.charAt(i) == separator) nrOfHits++;

  // First value is BPM
  bpm = getValue(btBuffer, ',', 0).toInt();
  bpm2ms = (60.0 / bpm) * 1000;

  Serial.print("Nr of hits: ");
  Serial.print(nrOfHits);
  Serial.print(", BPM: ");
  Serial.print(bpm);
  Serial.print(", BPM2MS: ");
  Serial.println(bpm2ms);

  for (int i=1; i<nrOfHits+1; i++) {
    int midx = i - 1;
    melody[midx] = getValue(btBuffer, ',', i).toInt();
  }
  melody[nrOfHits] = -1;

  Serial.print("Melody: ");
  for (int i=0; i<maxMelodySize; i++) {
    Serial.print(melody[i]);
    Serial.print(",");
  }
  currentNoteCount = 0;
  Serial.println();
}

String getValue(String data, char separator, int index) {
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}
