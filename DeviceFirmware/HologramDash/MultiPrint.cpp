#include "MultiPrint.h"
MultiPrint::MultiPrint(Print** targets, int length){
    this->targets = targets;
    this->length = length;
}

size_t MultiPrint::write(uint8_t value){
    for(int i = 0; i < this->length; i++){
        this->targets[i]->write(value);
    }
    return 0;
}