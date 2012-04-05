//
// $Id$
//
//
// Original author: Barbara Frewen <frewen@u.washington.edu>
//
// Copyright 2012 University of Washington - Seattle, WA 98195
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//

//Class definition for Spectrum

#include <iostream>
#include <cstdlib>
#include <ctime>
#include "Spectrum.h"


using namespace std;

namespace BiblioSpec {

Spectrum::Spectrum() :
    scanNumber_(0), 
    type_(SPEC_UNDEF), 
    mz_(0), 
    totalIonCurrentRaw_(-1),
    totalIonCurrentProcessed_(-1), 
    basePeakIntensityRaw_(-1), 
    basePeakIntensityProcessed_(-1)
{
}

Spectrum::Spectrum(const Spectrum& s) 
{
    scanNumber_ = s.scanNumber_;
    mz_ = s.mz_;
    type_ = s.type_;
    totalIonCurrentRaw_ = s.totalIonCurrentRaw_;
    totalIonCurrentProcessed_ = s.totalIonCurrentProcessed_;
    basePeakIntensityRaw_ = s.basePeakIntensityRaw_;
    basePeakIntensityProcessed_ = s.basePeakIntensityProcessed_;
    possibleCharges_.assign(s.possibleCharges_.begin(), s.possibleCharges_.end());
    setRawPeaks(s.rawPeaks_);
    setProcessedPeaks(s.processedPeaks_);

    srand(static_cast<unsigned>(time(0)));
}

//Destructor
Spectrum::~Spectrum()
{
}

void Spectrum::clear() {
    scanNumber_ = 0;
    mz_ = 0;
    type_ = SPEC_UNDEF;
    possibleCharges_.clear();
    rawPeaks_.clear();
    processedPeaks_.clear();
}

//Assignment operator
Spectrum& Spectrum::operator= (const Spectrum& right) 
{
    if(this == &right) return *this;  //check for self assignment

    scanNumber_ = right.scanNumber_;
    type_ = right.type_;
    mz_ = right.mz_;
    possibleCharges_ = right.possibleCharges_;
    rawPeaks_ = right.rawPeaks_;
    processedPeaks_ = right.processedPeaks_;
   
    return *this;
}

bool Spectrum::operator< (Spectrum otherSpec) {
    return mz_ < otherSpec.getMz();
}

//getters 
int Spectrum::getScanNumber() const
{
    return scanNumber_;
}
  
double Spectrum::getMz() const
{
    return mz_;
}

double Spectrum::getRetentionTime() const
{
    return retentionTime_;
}

int Spectrum::getNumRawPeaks() const
{ 
    return (int)rawPeaks_.size();
}

int Spectrum::getNumProcessedPeaks() const
{ 
    return (int)processedPeaks_.size();
}

double Spectrum::getTotalIonCurrentRaw() const
{
    if(totalIonCurrentRaw_ < 0){
        double sum = 0;
        for(size_t i = 0; i < rawPeaks_.size(); i++){
            sum += rawPeaks_[i].intensity;
        }
        return sum;
    }
    return totalIonCurrentRaw_;
}

double Spectrum::getTotalIonCurrentProcessed() const
{
    if(totalIonCurrentProcessed_ < 0){
        double sum = 0;
        for(size_t i = 0; i < processedPeaks_.size(); i++){
            sum += processedPeaks_[i].intensity;
        }
        return sum;
    }
    return totalIonCurrentProcessed_;
}

double Spectrum::getBasePeakIntensityRaw() const
{
    if( basePeakIntensityRaw_ < 0 ){
        return (*max_element(rawPeaks_.begin(), rawPeaks_.end(),
                             PeakIntLessThan())).intensity;
    }
    return basePeakIntensityRaw_;
}

double Spectrum::getBasePeakIntensityProcessed() const
{
    if( basePeakIntensityProcessed_ < 0 ){
        return (*max_element(processedPeaks_.begin(), 
                             processedPeaks_.end(),
                             PeakIntLessThan())).intensity;
    }
    return basePeakIntensityProcessed_;
}

double Spectrum::getBasePeakMzRaw() const
{
    return (*max_element(rawPeaks_.begin(), rawPeaks_.end(),
                         PeakIntLessThan())).mz;
}

double Spectrum::getBasePeakMzProcessed() const
{
    return (*max_element(processedPeaks_.begin(), processedPeaks_.end(),
                         PeakIntLessThan())).mz;
}

const vector<int>& Spectrum::getPossibleCharges() const
{
    return (const vector<int>&)possibleCharges_;
}

const vector<PEAK_T>& Spectrum::getRawPeaks() const
{
    return (const vector<PEAK_T>&)rawPeaks_;
}

const vector<PEAK_T>& Spectrum::getProcessedPeaks() const
{
    return (const vector<PEAK_T>&)processedPeaks_;
}
// deletes existing peaks
void Spectrum::setRawPeaks(const vector<PEAK_T>& newpeaks) {
    rawPeaks_.assign(newpeaks.begin(), newpeaks.end()); 
}

void Spectrum::setProcessedPeaks(const vector<PEAK_T>& newpeaks) {
    processedPeaks_.assign(newpeaks.begin(), newpeaks.end()); 
}

void Spectrum::setTotalIonCurrentRaw(double tic){
    totalIonCurrentRaw_ = tic;
}

void Spectrum::setTotalIonCurrentProcessed(double tic){
    totalIonCurrentProcessed_ = tic;
}

int Spectrum::mysize()
{
    return sizeof(*this);
}

void Spectrum::setScanNumber(int newNum) {
    scanNumber_ = newNum;
}

void Spectrum::setMz(double newmz){
    mz_ = newmz;
}

void Spectrum::setRetentionTime(double rt){
    retentionTime_ = rt;
}

/*
 * Adds an additional possible charge state to the spectrum.
 */
void Spectrum::addCharge(int newz) {
    possibleCharges_.push_back(newz);
}

float addIntensityToF(float n, PEAK_T p)
{ return n + p.intensity; }

} // namespace

 /*
  * Local Variables:
  * mode: c
  * c-basic-offset: 4
  * End:
 */
