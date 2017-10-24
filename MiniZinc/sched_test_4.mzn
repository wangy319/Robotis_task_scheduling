% Drilling holes

include "subcircuit.mzn";

int: tasks;

array [1..tasks] of int: d;
array [1..tasks] of string: names;

int: total = sum(i in 1..tasks)(d[i]);
array [1..tasks] of var 0..total: s;
var 0..total: end;

array[1..tasks+2,1..tasks+2] of int: graph;
array[1..tasks+2] of var 1..tasks+2:path;
% Everything has to happen before end
constraint
  forall(i in 1..tasks)(s[i] + d[i] <= end);

% Has to create a circuit
constraint subcircuit(path);

% The end is the sum of all the used durations (from graph)
end = sum(i in 1..tasks+2)(let{var int:j = path[i]} in graph[j,i]);

% Nodes that has to be in the circuit
constraint path[1] != 1 /\ path[10] != 10; % Start and end
constraint path[5] != 5 /\ path[7] != 7 /\ path[8] != 8 /\ path[9] != 9; % Drillings and moves


% Constraints or the start times
%constraint forall(i in 1..tasks)(foldl('\/',true,[s[i] >= s[j] + d[j]|j in 1..tasks where graph[j,i] > %0 /\ graph[j,i] < 100]));


solve minimize end;


output ["path = ", show(path), "\n"] ++ ["end = ", show(end), "\n"] ++
       [show(s[i]) ++ ", " ++ show(d[i]) ++ ", " ++ show(s[i]+d[i]) ++ ": " ++ show(names[i]) ++ "\n" | i in 1..tasks];