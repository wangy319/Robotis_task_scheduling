% Assembling a simple pen
int: machines = 2;
int: tasks;

array [1..tasks] of int: d;
array [1..tasks] of string: names;

int: total = sum(i in 1..tasks)(d[i]);
array [1..tasks] of var 0..total: s;
var 0..total: end;

array [1..tasks] of var 0..total: arm1_occ; % Time when the arm gets occupied
array [1..tasks] of var 0..total: arm1_de_occ; % Time when the arm is de-occupied
array [1..tasks] of var 0..total: arm2_occ; % Time when the arm gets occupied
array [1..tasks] of var 0..total: arm2_de_occ; % Time when the arm is de-occupied

% Everything has to happen before end
constraint
  forall(i in 1..tasks)(s[i] + d[i] <= end);

% Part numbers
% Head - 1
% Top - 2
% Body - 3
% Rod - 4
% Top-body - 5
% Top-body-pu - 6
% Head-rod - 7
% Head-rod-pu - 8
% Head-rod-top-body - 9

% Events occupying the arms cannot overlap
constraint
forall(j in 1..tasks)(
  forall(i in 1..tasks where i != j)(
    (arm1_de_occ[j] <= arm1_occ[i] \/ arm1_occ[j] >= arm1_de_occ[i]) /\
    (arm2_de_occ[j] <= arm2_occ[i] \/ arm2_occ[j] >= arm2_de_occ[i])
  )
);

% Take head i=1
constraint
  arm1_occ[1] = s[1];

% Take top i=2
constraint
  arm1_occ[2] = s[2];

% Take rod i=3
constraint
  arm2_occ[4] = s[3];

% Take body i=4
constraint
  arm2_occ[3] = s[4];

% Assemble top and body i=5
constraint % Can't happen before top and body is taken
  s[5] >= s[2] + d[2] /\ s[5] >= s[4] + d[4];
constraint
  arm1_de_occ[2] = s[5] + d[5] /\ arm2_de_occ[3] = s[5] + d[5] /\ arm2_occ[5] = s[5] + d[5];

% Drop top-body i=6
constraint % Can't happen before top-body is assembled
  s[6] >= s[5] + d[5];
constraint
  arm2_de_occ[5] = s[6] + d[6];

% Take top-body i=7
constraint % Can't happen before top-body is dropped
  s[7] >= s[6] + d[6];
constraint
  arm2_occ[6] = s[7];

% Assemble head and rod i=8
constraint % Can't happen before head is picked up and rod is picked up
  s[8] >= s[1] + d[1] /\ s[8] >= s[3] + d[3];
constraint
  arm1_de_occ[1] = s[8] + d[8] /\ arm2_de_occ[4] = s[8] + d[8] /\ arm1_occ[7] = s[8] + d[8];

% Drop head-rod i=9
constraint % Can't happen before head-rod is assembled
  s[9] >= s[8] + d[8];
constraint
  arm1_de_occ[7] = s[9] + d[9];

% Take head-rod i=10
constraint % Can't happen before head-rod is dropped
  s[10] >= s[9] + d[9];
constraint
  arm1_occ[8] = s[10];

% Assemble head-rod and top-body i=11
constraint % Can't happen before head-rod and top-body is taken
  s[11] >= s[7] + d[7] /\ s[11] >= s[10] + d[10];
constraint
  arm2_de_occ[6] = s[11] + d[11] /\ arm1_de_occ[8] = s[11] + d[11] /\ arm2_occ[9] = s[11] + d[11];

solve minimize end;

output ["end = ", show(end), "\n"] ++
       [show(s[i]) ++ ", " ++ show(d[i]) ++ ", " ++ show(s[i]+d[i]) ++ ": " ++ show(names[i]) ++ "\n" | i in 1..tasks];