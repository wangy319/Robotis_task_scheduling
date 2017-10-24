include "circuit.mzn";

int:nbrTasks=5;
int:nbrTools=2;
int:nbrMachines=2;
set of int: tasks=1..nbrTasks;
set of int:machines=1..nbrMachines;
array[1..nbrTasks+nbrMachines*2] of var 1..nbrTasks+nbrMachines*2:pred;
array[1..nbrTasks+nbrMachines*2] of var 1..nbrTasks+nbrMachines*2:orderedPred;
array[1..nbrTasks+nbrMachines*2] of var 1..nbrMachines:usingMachine;
int:nbrActingToolTypes=3;
% ActingToolTypes
% 1: grip
% 2: bottom-gripper
% 3: suction
array[1..nbrTools,1..nbrActingToolTypes] of int:actionTools=[|
1,0,0|
0,1,0|];
array[1..nbrTools,1..nbrActingToolTypes,1..nbrTasks+nbrMachines*2] of var 0..1000:toolLoad;
set of int: startTasks = nbrTasks+1..nbrTasks+nbrMachines;
set of int: goalTasks = nbrTasks+nbrMachines+1..nbrTasks+nbrMachines*2;
array[1..nbrTasks+nbrMachines*2] of int: toolNeeded=[1,1,2,2,1,0,0,0,0];
array[1..nbrTasks+nbrMachines*2] of var 1..nbrTools:toolUsed;


constraint
  orderedPred[1] = nbrTasks+1
  /\
  orderedPred[nbrTasks+nbrMachines*2] = nbrTasks+nbrMachines*2
  /\
  forall(task in 2..nbrTasks+nbrMachines*2)(
    pred[orderedPred[task]] = orderedPred[task-1]
  );

% Each start task gets assigned to a machine
constraint
  forall(machine in machines)(
    usingMachine[nbrTasks+machine] = machine
  );

% For all start tasks except the first one
constraint  
  forall(startTask in nbrTasks+2..nbrTasks+nbrMachines)(
    % The start tasks predecessor are the previous goal task
    pred[startTask] = startTask+nbrMachines-1
  );

% The first start tasks predecessor is the last goal task
constraint
  let{
    int: firstStartTask = nbrTasks+1,
    int: lastGoalTask = nbrTasks+nbrMachines*2
  }in
  pred[firstStartTask] = lastGoalTask;

constraint
  forall(task in 1..nbrTasks+nbrMachines*2 where toolNeeded[task] != 0)(
    toolUsed[task] = toolNeeded[task]
  );

constraint
  circuit(pred);

constraint
  forall(task in tasks union goalTasks)(
    usingMachine[task] = usingMachine[pred[task]]
  );

% Forcing two circuits
constraint
  forall(goalTask in goalTasks)(
    forall(startTask in startTasks)(
      pred[goalTask] != startTask /\ pred[pred[goalTask]] != startTask
    )
  );


constraint
  forall(tool in 1..nbrTools)(
    forall(actionTool in 1..nbrActingToolTypes)(
      forall(task in 1..nbrTasks+nbrMachines*2)(
        toolLoad[tool,actionTool,task] <= actionTools[tool,actionTool]
      )
    )
  );

% Initial loads
constraint
  forall(tool in 1..nbrTools)(
    forall(actionTool in 1..nbrActingToolTypes)(
      forall(startTask in startTasks)(
        toolLoad[tool,actionTool,startTask] = 0
      )
    )
  );

set of int:taking={1,3};
set of int:putting={2};
% Taking
constraint
trace("Taking:\n",
  forall(task in taking)(
trace("\t"++show(task)++"\n",
    toolLoad[toolUsed[task],toolUsed[task],task] = (toolLoad[toolUsed[task],toolUsed[task],pred[task]]+1)
)
  )
);

% Putting
constraint
trace("Putting:\n",
  forall(task in putting)(
trace("\t"++show(task)++"\n",
    toolLoad[toolUsed[task],toolUsed[task],pred[task]] > 0 /\ toolLoad[toolUsed[task],toolUsed[task],task] = (toolLoad[toolUsed[task],toolUsed[task],pred[task]]-1)
)
  )
);

% Others
constraint
trace("Others:\n",
  forall(task in tasks diff (taking union putting))(
trace("\t"++show(task)++"\n",
    toolLoad[toolUsed[task],toolUsed[task],task] = (toolLoad[toolUsed[task],toolUsed[task],pred[task]])
)
  )
);

solve satisfy;

output [show(pred)++"\n"++show(orderedPred)++"\n"++show(usingMachine)++"\n"++show(toolUsed)++"\n"++show(toolLoad)];