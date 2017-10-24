import java.io.IOException;
import java.util.ArrayList;

import org.jacop.core.Store;
import org.jacop.core.Var;
import org.jacop.fz.FlatzincLoader;
import org.jacop.search.CreditCalculator;
import org.jacop.search.DepthFirstSearch;
import org.jacop.search.SelectChoicePoint;
import org.jacop.search.TraceGenerator;

public class Test {
	public static void main(String[] args) throws IOException {
		long T1, T2, T;
		T1 = System.currentTimeMillis();

		if (args.length == 0) {
			args = new String[2];
			args[0] = "-s";
			args[1] = "/home/tommy/Desktop/Master Thesis/Workspace/constraintsandrobots/MiniZinc/Model6/model.fzn";
		}
		FlatzincLoader fl = new FlatzincLoader(args);
		fl.load();

		Store store = fl.getStore();

		// System.out.println (store);

		// System.out.println("============================================");
		// System.out.println(fl.getTables());
		// System.out.println("============================================");

		ArrayList<Var> tmp = new ArrayList<>();
		for (String key : store.variablesHashMap.keySet()) {
			if (key.startsWith("pred[")) {
				tmp.add(store.variablesHashMap.get(key));
			}
		}
		int nbrTasks = tmp.size();
		Var[] trace = new Var[nbrTasks];
		for (int i = 0; i < nbrTasks; i++) {
			int j = Integer
					.parseInt(tmp.get(i).id.split("\\[")[1].split("\\]")[0]);
			trace[j] = tmp.get(i);
		}
		for (Var v : trace) {
			System.out.println(v);
		}

		Var[] usingMachine = new Var[nbrTasks];
		for (String key : store.variablesHashMap.keySet()) {
			if (key.startsWith("usingMachine[")) {
				int i = Integer.parseInt(key.split("\\[")[1].split("\\]")[0]);
				usingMachine[i] = store.variablesHashMap.get(key);
			}
		}
		for (Var v : usingMachine) {
			System.out.println(v);
		}

		if (store.consistency()) {
			System.out.println("Store is consistent");
		} else {
			System.out.println("Store is not consistent");
		}

		System.out.println("\nIntVar store size: " + store.size()
				+ "\nNumber of constraints: " + store.numberConstraints());

		DepthFirstSearch<Var> label = fl.getDFS();
		SelectChoicePoint<Var> varSelect = fl.getSelectChoicePoint();
		Var cost = fl.getCost();

		// LDS<Var> lds = new LDS<Var>(30);
		// label.setExitChildListener(lds);

		int credits = 8, backtracks = 3, maxDepth = 50;
		CreditCalculator<Var> credit = new CreditCalculator<Var>(credits,
				backtracks, maxDepth);
		label.setConsistencyListener(credit);
		label.setExitChildListener(credit);
		label.setTimeOutListener(credit);

		TraceGenerator<Var> select = new TraceGenerator<Var>(label, varSelect,
				trace);

		boolean result = false;
		if (cost != null) {
			result = label.labeling(fl.getStore(), select, cost);
		} else
			result = label.labeling(fl.getStore(), select);

		fl.getSolve().statistics(result);

		// System.out.println(fl.getTables());

		// System.out.println(fl.getSearch());

		// System.out.println("cost: " + fl.getCost());

		if (result)
			System.out.println("*** Yes");
		else
			System.out.println("*** No");

		T2 = System.currentTimeMillis();
		T = T2 - T1;
		System.out.println("\n\t*** Execution time = " + T + " ms");
	}

}
