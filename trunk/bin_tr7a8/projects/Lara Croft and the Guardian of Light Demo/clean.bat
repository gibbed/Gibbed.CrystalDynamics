@echo off
T:\Utilities\bin\sort.exe test.filelist > test.filenew
T:\Utilities\bin\uniq.exe test.filenew > test.filelist
del test.filenew
