# Author: Tobi Vollebregt

.PHONY : all clean

MCS=gmcs
MCS_FLAGS=-r:System.Data.dll,System.Drawing.dll,System.Windows.Forms.dll

SRC=$(wildcard *.cs */*.cs)
EXE=springie.exe

all: $(EXE)

clean:
	@$(RM) $(EXE)

$(EXE): $(SRC)
	@$(MCS) $(MCS_FLAGS) -out:$@ $^ 
