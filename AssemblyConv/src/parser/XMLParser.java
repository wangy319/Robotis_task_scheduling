package parser;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.ArrayList;

import javax.xml.parsers.ParserConfigurationException;
import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

import objects.Assembly;
import objects.Component;
import objects.Fixture;
import objects.Machine;
import objects.Output;
import objects.Task;
import objects.Tool;
import objects.Tray;

import org.xml.sax.SAXException;

import timematrix.TimeMatrix;

public class XMLParser {

	public static Assembly parse(File xml, TimeMatrix timeMatrix)
			throws SAXException, IOException, ParserConfigurationException,
			XMLStreamException {
		Assembly assembly = new Assembly(timeMatrix);
		XMLInputFactory factory = XMLInputFactory.newInstance();
		XMLStreamReader reader = factory
				.createXMLStreamReader(new FileInputStream(xml));

		while (reader.hasNext()) {
			int event = reader.next();

			switch (event) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Tray":
					Tray t = new Tray(parseId(reader));
					assembly.addTray(t);
					break;
				case "Output":
					Output o = new Output(parseId(reader));
					assembly.addOutput(o);
					break;
				case "Fixture":
					Fixture f = new Fixture(parseId(reader));
					assembly.addFixture(f);
					break;
				case "Component":
					Component c = new Component(parseId(reader));
					assembly.addComponent(c);
					break;
				case "Subcomponents":
					parseSubcomponents(reader, assembly);
					break;
				case "Tool":
					Tool tool = new Tool(parseId(reader));
					assembly.addTool(tool);
					break;
				case "Machine":
					Machine m = new Machine(parseId(reader));
					assembly.addMachine(m);
					break;
				case "Task":
					parseTask(reader, assembly);
					break;
				case "OrderedGroup":
					parseOrderedGroup(reader, assembly);
					break;
				case "ConcurrentGroup":
					parseConcurentGroup(reader, assembly);
					break;
				case "TasksOutOfRange":
					parseTasksOutOfRange(reader, assembly);
					break;
				case "ToolChangeDurations":
					parseToolChangeDurations(reader, assembly);
					break;
				}

			}
		}
		assembly.doneParsing();
		return assembly;
	}

	private static String parseId(XMLStreamReader reader)
			throws XMLStreamException {
		return reader.getAttributeValue(null, "id");
	}

	private static void parseSubcomponents(XMLStreamReader reader,
			Assembly assembly) throws XMLStreamException {
		Component comp = assembly.getComponent(parseId(reader));
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Component":
					String subcompId = parseId(reader);
					if (!assembly.componentExists(subcompId)) {
						throw new IllegalArgumentException("Component "
								+ subcompId + " used without being declared");
					}
					comp.addSubcomponent(assembly.getComponent(subcompId));
					break;
				}
			case XMLStreamConstants.END_ELEMENT:
				switch (reader.getLocalName()) {
				case "Subcomponents":
					return;
				default:
					break;
				}
			}
		}
	}

	private static void parseTask(XMLStreamReader reader, Assembly assembly)
			throws XMLStreamException {
		Task task = new Task(parseId(reader), Integer.parseInt(reader
				.getAttributeValue(null, "Duration")));
		String id = null;
		String action = null;
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Tray":
					id = parseId(reader);
					if (assembly.trayExists(id)) {
						task.tray = assembly.getTray(id);
					} else {
						throw new IllegalArgumentException("Tray " + id
								+ " used without being declared");
					}
					break;
				case "Fixture":
					id = parseId(reader);
					if (assembly.fixtureExists(id)) {
						task.fixture = assembly.getFixture(id);
					} else {
						throw new IllegalArgumentException("Fixture " + id
								+ " used without being declared");
					}
					break;
				case "Output":
					id = parseId(reader);
					if (assembly.outputExists(id)) {
						task.output = assembly.getOutput(id);
					} else {
						throw new IllegalArgumentException("Output " + id
								+ " used without being declared");
					}
					break;
				case "Component":
					id = parseId(reader);
					if (assembly.componentExists(id)) {
						task.componentsUsed.add(assembly.getComponent(id));
					} else {
						throw new IllegalArgumentException("Component " + id
								+ " used without being declared");
					}
					break;
				case "ComponentCreated":
					id = parseId(reader);
					if (assembly.componentExists(id)) {
						task.componentCreated = assembly.getComponent(id);
					} else {
						throw new IllegalArgumentException("Component " + id
								+ " used without being declared");
					}
					break;
				case "ToolNeeded":
					id = parseId(reader);
					if (assembly.toolExists(id)) {
						task.toolNeeded = assembly.getTool(id);
					} else {
						throw new IllegalArgumentException("Tool " + id
								+ " used without being declared");
					}
					break;
				case "Action":
					action = parseId(reader).toLowerCase();
					if (Task.allowedActions.contains(action)) {
						task.action = action;
					} else {
						throw new IllegalArgumentException("Action \"" + action
								+ "\" is not valid");
					}
					break;

				default:
					break;
				}
			case XMLStreamConstants.END_ELEMENT:
				switch (reader.getLocalName()) {
				case "Task":
					assembly.addTask(task);
					return;
				}
			}
		}
	}

	private static void parseTasksOutOfRange(XMLStreamReader reader,
			Assembly assembly) throws XMLStreamException {
		Machine m;
		String id = parseId(reader);
		if (assembly.machineExists(id)) {
			m = assembly.getMachine(id);
		} else {
			throw new IllegalArgumentException("Machine " + id
					+ " used without being declared");
		}
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Task":
					m.addOutOfRangeTask(assembly.getTask(parseId(reader)));
					break;
				}
			case XMLStreamConstants.END_ELEMENT:
				switch (reader.getLocalName()) {
				case "TasksOutOfRange":
					return;
				}
			}
		}
	}

	private static void parseOrderedGroup(XMLStreamReader reader,
			Assembly assembly) throws XMLStreamException {
		String id = null;
		ArrayList<Task> group = new ArrayList<>();
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Task":
					id = parseId(reader);
					if (assembly.taskExists(id)) {
						group.add(assembly.getTask(id));
					} else {
						throw new IllegalArgumentException("Task " + id
								+ " used without being declared");
					}
				}
			case XMLStreamConstants.END_ELEMENT:
				switch (reader.getLocalName()) {
				case "OrderedGroup":
					assembly.addOrderedGroup(group);
					return;
				}
			}
		}
	}

	private static void parseConcurentGroup(XMLStreamReader reader,
			Assembly assembly) throws XMLStreamException {
		String id = null;
		ArrayList<Task> group = new ArrayList<>();
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Task":
					id = parseId(reader);
					if (assembly.taskExists(id)) {
						group.add(assembly.getTask(id));
					} else {
						throw new IllegalArgumentException("Task " + id
								+ " used without being declared");
					}
				}
			case XMLStreamConstants.END_ELEMENT:
				switch (reader.getLocalName()) {
				case "ConcurrentGroup":
					assembly.addConcurrentGroup(group);
					return;
				}
			}
		}
	}

	private static void parseToolChangeDurations(XMLStreamReader reader,
			Assembly assembly) throws NumberFormatException, XMLStreamException {
		while (reader.hasNext()) {
			switch (reader.next()) {
			case XMLStreamConstants.START_ELEMENT:
				switch (reader.getLocalName()) {
				case "Change":
					String fromToolId = reader.getAttributeValue(null,
							"FromToolId");
					String toToolId = reader
							.getAttributeValue(null, "ToToolId");
					if (!assembly.toolExists(fromToolId)) {
						throw new IllegalArgumentException("Tool " + fromToolId
								+ " used without being declared");
					}
					if (!assembly.toolExists(toToolId)) {
						throw new IllegalArgumentException("Tool " + toToolId
								+ " used without being declared");
					}
					int duration = Integer.parseInt(reader.getAttributeValue(
							null, "Duration"));
					assembly.addToolChangeDuration(
							assembly.getTool(fromToolId),
							assembly.getTool(toToolId), duration);
					break;
				}
			}
		}
	}
}
