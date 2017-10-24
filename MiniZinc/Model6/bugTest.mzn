include "circuit.mzn";

array[1..5] of var 1..5:a;

constraint
	circuit(a);

solve::int_search([], first_fail, indomain_max, complete)satisfy;

output [show(a)];
