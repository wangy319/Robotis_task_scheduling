% Assembling a simple pen
int: machines = 2;
int: tasks;

array [1..tasks] of int: d;
array [1..tasks] of string: names;

int: total = sum(i in 1..tasks)(d[i]);
array [1..tasks] of var 0..total: s;
var 0..total: end;

array [1..tasks] of var 0..total: arm1_occ_start;
array [1..tasks] of var 0..total: arm1_occ_dur;
array [1..tasks] of var 0..total: arm2_occ_start;
array [1..tasks] of var 0..total: arm2_occ_dur;

% Everything has to happen before end
constraint
  forall(i in 1..tasks)(s[i] + d[i] <= end);

% i:s that take up arm-resources
% Some arm has to be able to take the part
constraint
  forall(j in [1,2,3,4])(
    (arm1_occ_start[j] = s[j] /\ arm1_occ_dur[j] = d[j]/\
  forall(i in 1..tasks where i != j)((s[j] < arm1_occ_start[i] /\ s[j] + d[j] < arm1_occ_start[i]) \/ (s[j] > arm1_occ_start[i] + arm1_occ_dur[i] /\ s[j] + d[j] > arm1_occ_start[i] + arm1_occ_dur[i])))
  \/
  (arm2_occ_start[j] = s[j] /\ arm2_occ_dur[j] = d[j]/\
  forall(i in 1..tasks where i != j)((s[j] < arm2_occ_start[i] /\ s[j] + d[j] < arm2_occ_start[i]) \/ (s[j] > arm2_occ_start[i] + arm2_occ_dur[i] /\ s[j] + d[j] > arm2_occ_start[i] + arm2_occ_dur[i])))
);

% Take head i=1

% Take top i=2

% Take body i=3

% Take rod i=4

% Assemble top and body i=5
constraint % Can't happen before top and body is taken
  s[5] >= s[2] + d[2] /\ s[5] >= s[3] + d[3];

% Drop top and body i=6

% Assemble head and rod i=7
constraint % Can't happen before head is picked up and rod is picked up
  s[7] >= s[1] + d[1] /\ s[7] >= s[4] + d[4];

% Drop head and rod i=8

% Assemble head-rod and top-body i=9
constraint % Can't happen before head is picked up and rod is picked up
  s[9] >= s[5] + d[5] /\ s[9] >= s[7] + d[7];

solve minimize end;

output ["end = ", show(end), "\n"] ++
       [show(s[i]) ++ ", " ++ show(d[i]) ++ ", " ++ show(s[i]+d[i]) ++ ": " ++ show(names[i]) ++ "\n" | i in 1..tasks];