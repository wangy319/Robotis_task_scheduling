include "cumulative.mzn";
include "member.mzn";
include "alldifferent_except_0.mzn";

int:nbrTasks;
int:nbrMachines;
int:nbrTools;
int:nbrFixtures;
int:nbrComponents;
int:nbrCycles;
int:nbrUniqueTasks = nbrTasks div nbrCycles;
set of int: tasks = 1..nbrTasks;
set of int: machines = 1..nbrMachines;
set of int: tools = 1..nbrTools;

array[tasks] of string:name;
array[tasks] of int:duration;
array[1..nbrUniqueTasks] of set of int:identicalTasks;
array[tasks] of int:cycleNbr;
array[tasks] of int:tray;
array[tasks] of int:out_put;
array[tasks] of int:fixture;
array[tasks] of set of int:componentsUsed;

array[tasks] of int:componentCreated;

set of int:putting;
set of int:mounting;
set of int:taking;
set of int:moving;

int:nbrConcurrentGroups;
array[1..nbrConcurrentGroups] of set of int:concurrentTasks;

int:orderedGroups;
int:maxOrderedLength;
array[1..orderedGroups,1..maxOrderedLength] of int:order;

array[tasks] of int: toolNeeded; % The tool nedded for each task. 0 means no specific tool is required

array[tools,tools] of int: changeToolDuration;

set of int:fixtures = {fixture[i]|i in tasks where fixture[i] > 0}; % The set of fixtures available
set of int:trays = {tray[i]|i in tasks where tray[i] > 0}; % The set of trays available
set of int:components = 1..nbrComponents;
set of int:cycles = {cycleNbr[i]|i in tasks}; % The set of cycles used
array[components] of set of int:putting_c = [{task|task in putting where c in componentsUsed[task]}|c in components];
array[components] of set of int:mounting_c = [{task|task in mounting where c in componentsUsed[task]}|c in components];
array[components] of set of int:taking_c = [{task|task in taking where c in componentsUsed[task]}|c in components];
array[components] of set of int:moving_c = [{task|task in moving where c in componentsUsed[task]}|c in components];
array[components] of set of int:actions_c = [putting_c[c] union mounting_c[c] union taking_c[c] union moving_c[c]|c in components];

set of int: orderedSet = {order[g,t]|g in 1..orderedGroups, t in 1..maxOrderedLength where order[g,t] != 0};

array[tasks] of var machines: usingMachine; % The machine each task uses
array[1..nbrTasks+1,tasks] of int:timeToTask; % The time it takes to move from one task to another
array[1..nbrTasks+1] of int:timeToChange;
array[tasks] of int:timeFromChange;
array[tasks] of var 0..nbrTasks:pred; % The predecessor task for each task. The first tasks has predecessor 0
array[tasks] of var 0..1:hasToolChangeBefore;

array[1..nbrFixtures,tasks] of var 0..nbrTasks:fixtureOccupation;

int: total = sum(i in tasks)(duration[i]);
array [tasks] of var 0..total*1000: start;
array [tasks] of var 0..total*1000: end;
var 0..total*1000: total_end;

array[tasks] of var 0..total*2:moveStart;
array[tasks] of var 0..total*2:moveDuration;
array[tasks] of var 0..total*2:moveEnd;

array[tasks] of var 0..nbrTools:toolUsed;

array[tasks] of var 0..nbrTasks:succ;

%-----------------------------------------------------------------------------------%
% Predicates
predicate no_overlap(int:task1, int:task2) =
  no_overlap_2(start[task1], end[task1], start[task2], end[task2]);

predicate no_overlap_2(var int:start1, var int:end1, var int:start2, var int:end2) =
  end1 <= start2 \/ start1 >= end2;

predicate task1EndBeforeTask2Start(int:task1, int:task2) =
  moveEnd[task1] <= start[task2] /\ end[task1] <= start[task2] /\ end[task1] <= moveStart[task2] /\ moveEnd[task1] <= moveStart[task2] /\ pred[task1] != task2 /\ succ[task2] != task1;

predicate task1EndBeforeTask2Start(var int:task1, int:task2) =
  moveEnd[task1] <= start[task2] /\ end[task1] <= start[task2] /\ end[task1] <= moveStart[task2] /\ moveEnd[task1] <= moveStart[task2] /\ pred[task1] != task2 /\ succ[task2] != task1;

predicate task1PredecessorOfTask2(int:task1, int:task2) =
  pred[task2] = task1 /\ pred[task1] != task2 /\ pred[task1] != pred[task2] /\ succ[task1] = task2 /\ succ[task2] != task1 /\ succ[task1] != succ[task2];

%-----------------------------------------------------------------------------------%
%Constraints

% The end is were the last task ends
constraint
  total_end = max(end);

% End times
constraint
  forall(i in tasks)(
    end[i] = start[i] + duration[i]
  );

constraint
  forall(i in tasks)(
    moveEnd[i] = moveStart[i] + moveDuration[i]
  );

% Precedences
% If a set of tasks on a component involves a mount task and a put task, the put task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(put_task in putting_c[comp])(
        task1EndBeforeTask2Start(put_task, mount_task)
      )
    )
  );

% If a set of tasks on a component involves a mount task and a take task, the take task has to come before the mount task
constraint
  forall(comp in components)(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
        task1EndBeforeTask2Start(take_task, mount_task)
      )
    )
  );

% If a component has a put and take performed on it in a tray, the put has to come before the take
constraint
  forall(comp in components)(
    forall(putTask in putting_c[comp] where tray[putTask] > 0)(
      forall(takeTask in taking_c[comp] where tray[putTask] = tray[takeTask])(
        task1EndBeforeTask2Start(putTask, takeTask)
      )
    )
  );


% If a set of tasks on a component involves a mount and a take task, but no move tasks or put task on the same fixture, tray or output as the mount, the take task is the predecessor of the mount task
constraint
  forall(comp in components)(%putting_c[comp] = {} /\  where moving_c[comp] = {}
    forall(mount_task in mounting_c[comp])(
      let{
        set of int: puts={p|p in putting_c[comp] where (fixture[p] > 0 /\ fixture[p] = fixture[mount_task]) \/ (out_put[p] > 0 /\ out_put[p] = out_put[mount_task]) \/ (tray[p] > 0 /\ tray[p] = tray[mount_task])}
      } in
      forall(take_task in taking_c[comp] where not(take_task in orderedSet) /\ puts = {})(
trace("mount:"++show(name[mount_task])++" take:"++show(name[take_task])++"\n",
          task1PredecessorOfTask2(take_task, mount_task)
)
      )
    )
  );

% If a set of tasks on a component involves a mount task, a take task, and one or several moving tasks, but no put task, the take task has to come before the mount task, and the move tasks come inbetween
constraint
  forall(comp in components where putting_c[comp] = {} /\ moving_c[comp] != {})(
    forall(mount_task in mounting_c[comp])(
      forall(take_task in taking_c[comp])(
          task1EndBeforeTask2Start(take_task, mount_task) /\ 
          pred[mount_task] != take_task /\
          usingMachine[take_task] = usingMachine[mount_task] /\
          % The take task has to happen before the moving tasks and the mount task afterwards
          forall(task in moving_c[comp])(
            task1EndBeforeTask2Start(take_task, task) /\ task1EndBeforeTask2Start(task, mount_task)
          )
          /\
          % No other non-move task can come between the take task and the mount
          forall(task in tasks diff {mount_task, take_task} where not(task in moving))(
            usingMachine[take_task] = usingMachine[task] -> no_overlap_2(start[task], end[task],  start[take_task], end[mount_task])
          )
      )
    )
  );

% If a set of tasks on a component involves a put task in a tray and a take task, the take task ha to be the predecessor of the put task
constraint
  forall(comp in components)(
    forall(put_task in putting_c[comp] where tray[put_task] = 0)(
      forall(take_task in taking_c[comp])(
        task1PredecessorOfTask2(take_task, put_task)
      )
    )
  );

% Between two put tasks on the same fixture, there has to be at least one take task
constraint
  forall(putTask1 in tasks where putTask1 in putting /\ fixture[putTask1] > 0)(
    forall(putTask2 in tasks where putTask2 != putTask1 /\ putTask2 in putting /\ fixture[putTask1] = fixture[putTask2])(
trace("putTask1:"++show(name[putTask1])++" putTask2:"++show(name[putTask2])++"\n",
      start[putTask2] >= end[putTask1] -> exists(takeTask in tasks where takeTask in taking /\ fixture[putTask1] = fixture[takeTask])(
trace("takeTask:"++show(name[takeTask])++"\n",
        start[takeTask] >= end[putTask1] /\ end[takeTask] <= start[putTask2]
)
      )
)
    )
);

% constraint
%   forall(putTask1 in tasks where putTask1 in putting /\ fixture[putTask1] > 0)(
%     forall(putTask2 in tasks where putTask2 != putTask1 /\ putTask2 in putting /\ fixture[putTask1] = fixture[putTask2])(
% trace("putTask1:"++show(name[putTask1])++" putTask2:"++show(name[putTask2])++"\n",
%       exists(takeTask in tasks where takeTask in taking /\ fixture[putTask1] = fixture[takeTask])(
% trace("takeTask:"++show(name[takeTask])++"\n",
%         (start[takeTask] >= end[putTask1] /\ end[takeTask] <= start[putTask2]) \/ (start[takeTask] >= end[putTask2] /\ end[takeTask] <= start[putTask1])
% )
%       )
% )
    

%     )
% );





% Concurrent tasks
constraint
  forall(group in 1..nbrConcurrentGroups)(
    forall(task1 in concurrentTasks[group])(
      forall(task2 in concurrentTasks[group] diff {task1})(
        start[task1] = start[task2] /\ usingMachine[task1] != usingMachine[task2] /\ pred[task1] != task2 /\ pred[task2] != task1 /\ pred[task1] != pred[task2] /\ succ[task1] != succ[task2] /\ succ[task2] != task1 /\ succ[task1] != task2
      )
    )
  );

% Components cannot be used before they are created
constraint
  forall(task1 in tasks where componentCreated[task1] > 0)(
    forall(task2 in tasks where componentCreated[task1] in componentsUsed[task2])(
      task1EndBeforeTask2Start(task1, task2)
    )
  );

% Order
constraint
  forall(group in 1..orderedGroups)(
    forall(t in 1..maxOrderedLength-1 where order[group,t+1] != 0)(
      let{
        int:task1 = order[group,t],
        int:task2 = order[group,t+1],
      } in
       task1PredecessorOfTask2(task1, task2)
    )
  );

% Two tasks on the same fixture can't overlap
constraint
  forall(f in fixtures)(
    forall(task1 in tasks where fixture[task1] = f)(
      forall(task2 in tasks diff {task1} where fixture[task1] = fixture[task2])(
        no_overlap(task1, task2)
      )
    )
  );

% Two tasks on the same tray can't overlap
constraint
  forall(t in trays)(
    forall(task1 in tasks where tray[task1] = t)(
      forall(task2 in tasks diff {task1} where tray[task1] = tray[task2])(
        no_overlap(task1, task2)
      )
    )
  );

% Two tasks using the same machine cannot overlap
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] = usingMachine[task2] -> no_overlap(task1, task2)
    )
  );

% There can only be as many tasks performed simultaneously as there are machines
 constraint
   cumulative(start, duration, [1|t in tasks], nbrMachines);


% Predecessors

% A task cannot be a predecessor to itself
constraint
  forall(task in tasks)(
    pred[task] != task /\ succ[task] != task
  );

% A task can only start after the move to it
constraint
  forall(task in tasks)(
    start[task] >= moveEnd[task]
  );

% Apart from the first tasks, a task has to start after its predecessor
constraint
   forall(task in tasks)(
    pred[task] != 0 -> task1EndBeforeTask2Start(pred[task], task)
  );

% A task cannot be a predecessor to its predecessor
constraint
  forall(task in tasks)(
    pred[task] != 0 -> pred[pred[task]] != task
  );

% Since there are multiple machines, there can be multiple start tasks
% Unnecessary?
constraint
  alldifferent_except_0(pred);
constraint
  alldifferent_except_0(succ);


% Tasks on the same machine cannot have the same predecessor
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] = usingMachine[task2] -> pred[task1] != pred[task2] /\ succ[task1] != succ[task2]
    )
  );

% The start tasks have predecessor 0 and starts at time 0
constraint
  forall(task in tasks)(
    pred[task] = 0 -> moveStart[task] = 0
  );


% Two tasks using different machines cannot be each other predecessors
constraint
  forall(task1 in tasks)(
    forall(task2 in tasks diff {task1})(
      usingMachine[task1] != usingMachine[task2] -> pred[task1] != task2 /\ pred[task2] != task1 /\ succ[task1] != task2 /\ succ[task2] != task1
    )
  );

% A task and its predecessor has to use the same machine
constraint
  forall(task in tasks)(
    pred[task] != 0 -> usingMachine[task] = usingMachine[pred[task]]
  );

% There has to be at least one starting task
constraint
  member(pred,0);

% There has to be at least one ending task
constraint
  member(succ,0);

% No other task on the same machine can come between task i and its predecessor pred[i], i.e. no other task can overlap the timeslot inbetween
constraint
  forall(task in tasks)(
    pred[task] != 0 -> forall(task2 in tasks)( usingMachine[task2] = usingMachine[task] -> no_overlap_2(start[task2], end[task2], end[pred[task]], start[task]) )
  );

% % Assigns the duration for the move between tasks
% constraint
%   forall(task in tasks)(
%     moveDuration[task] = timeToTask[pred[task]+1,task]
%   );


% Tools
% constraint
%   forall(task in tasks)(
%     pred[task] = 0 -> moveDuration[task] = timeToTask[1,task]
%   );

% % All other tasks has to have the same tool as its predecessor or do a change first
% constraint
%   forall(task in tasks)(
%     pred[task] != 0 -> ((toolUsed[task] = toolUsed[pred[task]] /\ moveDuration[task] = timeToTask[pred[task]+1,task]) \/ (moveDuration[task] = timeToChange[pred[task]+1] + changeToolDuration[toolUsed[pred[task]],toolUsed[task]] + timeFromChange[task] /\ hasToolChangeBefore[task] = 1))
%   );

% All other tasks has to have the same tool as its predecessor or do a change first
constraint
  forall(task in tasks)(
    (pred[task] = 0 /\ moveDuration[task] = timeToTask[1,task]) \/ (pred[task] != 0 /\ ((toolUsed[task] = toolUsed[pred[task]] /\ moveDuration[task] = timeToTask[pred[task]+1,task]) \/ (moveDuration[task] = timeToChange[pred[task]+1] + changeToolDuration[toolUsed[pred[task]],toolUsed[task]] + timeFromChange[task] /\ hasToolChangeBefore[task] = 1)))
  );


constraint
  forall(task in tasks where toolNeeded[task] != 0)(
    toolUsed[task] = toolNeeded[task]
  );

% Tasks that does not require any special tool has to either use the same tool as its predecessor or as its successor
constraint
  forall(task in tasks where toolNeeded[task] = 0)(
    pred[task] != 0 -> toolUsed[task] = toolUsed[pred[task]] \/ succ[task] != 0 -> toolUsed[task] = toolUsed[succ[task]]
  );

% Cycle order
constraint
  forall(it in identicalTasks)(
    forall(task1 in it)(
      forall(task2 in it where cycleNbr[task1] < cycleNbr[task2])(
          task1EndBeforeTask2Start(task1, task2)
      )
    )
  );

solve :: seq_search([
                      int_search(usingMachine, first_fail, indomain, complete),
                      int_search(pred, first_fail, indomain_random, complete),
                      int_search(succ, first_fail, indomain, complete),
                      int_search(moveStart, smallest, indomain_split, complete),
                      int_search(start, smallest, indomain_split, complete)
])
        minimize total_end;
% solve minimize total_end;

output ["End = " ++ show(total_end) ++ "\n"] ++ 
["start duration i usingMachine pred name hasToolChangeBefore\n"] ++
[show(moveStart[i]) ++ " " ++ show(moveDuration[i]) ++ " 0 0 0" ++ " Move from " ++ show(pred[i]) ++ " to " ++ show(i) ++ "\n" ++
show(start[i]) ++ " " ++ show(duration[i]) ++ " " ++ show(i) ++ " " ++ show(usingMachine[i]) ++ " " ++ show(pred[i]) ++ " " ++ show(name[i]) ++ " TS bef-" ++ show(hasToolChangeBefore[i]) ++ " TU" ++ show(toolUsed[i]) ++" succ-"++show(succ[i])++ "\n"|i in tasks]++[show(fixtureOccupation)];

