import sys
import re

f = open(sys.argv[1],'r').readlines()
fout = open(sys.argv[1] + '_conv','w')
fout.write('[|')
for line in f:
	m = re.match(',"?[^\d]+"?,((?:\d,?)+)',line)
	if m is not None:
		print m.group()
		fout.write(m.group(1)+'|\n')
fout.write(']')
fout.close()
