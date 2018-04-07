#ifndef MultiPrint_h
#define MultiPrint_h

#include "Arduino.h"
#include "Print.h"

class MultiPrint : public Print
{
  public:
    MultiPrint(Print** targets, int length);
    size_t write(uint8_t x);
  private:
    Print** targets;
    int length;
};

#endif
