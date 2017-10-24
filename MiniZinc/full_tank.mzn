include "subcircuit.mzn";

int: cities = 10;
array[1..cities] of int: fuel_price = [10, 10, 8, 12, 13, 9, 10, 11, 12, 8];
array[1..cities, 1..cities] of int: distance = [|0, 10, 11, 6, 100, 100, 100, 100, 100, 0
                                                |10, 0, 5, 8, 100, 100, 100, 100, 100, 100
                                                |11, 5, 0, 3, 5, 100, 100, 100, 100, 100
                                                |6, 8, 100, 0, 2, 6, 7, 100, 100, 100 
                                                |100, 100, 3, 2, 0, 100, 12, 100, 100, 100
                                                |100, 100, 5, 6, 100, 0, 100, 14, 100, 100
                                                |100, 100, 100, 7, 12, 100, 0, 5, 3, 100
                                                |100, 100, 100, 100, 100, 14, 5, 0, 1, 9
                                                |100, 100, 100, 100, 100, 100, 3, 1, 0, 2
                                                |100, 100, 100, 100, 100, 100, 100, 9, 2, 0
                                                |];
int: total_price = sum(i in 1..cities)(fuel_price[i]);
var 1..total_price: cost = sum(i in 1..cities)(let{var int:j = path[i]} in distance[j,i]*fuel_price[j]);
int: tank_cap = 10;

%array[1..cities, 1..cities] of int: refill_costs = [[0-(tank_cap-distance[i,j])*fuel_price[j]|j in %1..cities]|i in 1..cities];

constraint path[1] != 1 /\ path[10] != 10;

array[1..cities] of var 1..cities: path;
constraint subcircuit(path);

%constraint subcurcuit([i|i in 1..cities where
%i = [min(
%[0-(tank_cap-distance[i,j])*fuel_price[j]|j in 1..cities]
%)]
%);

solve minimize cost;

output ["cost = "++show(cost)++"\npath = "++show(path)];