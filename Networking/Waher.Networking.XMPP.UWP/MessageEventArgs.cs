﻿using System;
using System.Collections.Generic;
using System.Xml;
using Waher.Content.Xml;

namespace Waher.Networking.XMPP
{
	/// <summary>
	/// Type of message received.
	/// </summary>
	public enum MessageType
	{
		/// <summary>
		/// The message is sent in the context of a one-to-one chat session.  Typically an interactive client will present a message
		/// of type "chat" in an interface that enables one-to-one chat between the two parties, including an appropriate conversation
		/// history.  Detailed recommendations regarding one-to-one chat sessions are provided under Section 5.1.
		/// </summary>
		Chat,

		/// <summary>
		/// The message is generated by an entity that experiences an error when processing a message received from another entity (for
		/// details regarding stanza error syntax, refer to [XMPP-CORE]).  A client that receives a message of type "error" SHOULD present an
		/// appropriate interface informing the original sender regarding the nature of the error.
		/// </summary>
		Error,

		/// <summary>
		/// The message is sent in the context of a multi-user chat environment (similar to that of [IRC]).  Typically a
		/// receiving client will present a message of type "groupchat" in an interface that enables many-to-many chat between the parties,
		/// including a roster of parties in the chatroom and an appropriate conversation history.  For detailed information about XMPP-based
		/// groupchat, refer to [XEP-0045].
		/// </summary>
		GroupChat,

		/// <summary>
		/// The message provides an alert, a notification, or other transient information to which no reply is expected (e.g.,
		/// news headlines, sports updates, near-real-time market data, or syndicated content).  Because no reply to the message is expected,
		/// typically a receiving client will present a message of type "headline" in an interface that appropriately differentiates the
		/// message from standalone messages, chat messages, and groupchat messages (e.g., by not providing the recipient with the ability to
		/// reply).  If the 'to' address is the bare JID, the receiving server SHOULD deliver the message to all of the recipient's available
		/// resources with non-negative presence priority and MUST deliver the message to at least one of those resources; if the 'to' address is
		/// a full JID and there is a matching resource, the server MUST deliver the message to that resource; otherwise the server MUST
		/// either silently ignore the message or return an error (see Section 8).
		/// </summary>
		Headline,

		/// <summary>
		/// The message is a standalone message that is sent outside the context of a one-to-one conversation or groupchat, and to
		/// which it is expected that the recipient will reply.  Typically a receiving client will present a message of type "normal" in an
		/// interface that enables the recipient to reply, but without a conversation history.  The default value of the 'type' attribute
		/// is "normal".
		/// </summary>
		Normal,
	}

	/// <summary>
	/// Event arguments for message events.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		private KeyValuePair<string, string>[] bodies;
		private KeyValuePair<string, string>[] subjects;
		private IEndToEndEncryption e2eEncryption = null;
		private XmlElement message;
		private XmlElement content;
		private XmlElement errorElement = null;
		private ErrorType errorType = ErrorType.None;
		private XmppException stanzaError = null;
		private string errorText = string.Empty;
		private XmppClient client;
		private XmppComponent component;
		private MessageType type;
		private string threadId;
		private string parentThreadId;
		private string from;
		private string fromBareJid;
		private string to;
		private string id;
		private string body;
		private string subject;
		private int errorCode;
		private bool ok;

		/// <summary>
		/// Event arguments for message events.
		/// </summary>
		/// <param name="e">Values are taken from this object.</param>
		public MessageEventArgs(MessageEventArgs e)
		{
			this.bodies = e.bodies;
			this.subjects = e.subjects;
			this.message = e.message;
			this.content = e.content;
			this.errorElement = e.errorElement;
			this.errorType = e.errorType;
			this.stanzaError = e.stanzaError;
			this.errorText = e.errorText;
			this.client = e.client;
			this.component = e.component;
			this.type = e.type;
			this.threadId = e.threadId;
			this.parentThreadId = e.parentThreadId;
			this.from = e.from;
			this.fromBareJid = e.fromBareJid;
			this.to = e.to;
			this.id = e.id;
			this.body = e.body;
			this.subject = e.subject;
			this.errorCode = e.errorCode;
			this.ok = e.ok;
		}

		/// <summary>
		/// Event arguments for message events.
		/// </summary>
		/// <param name="Client">Client</param>
		/// <param name="Message">Message element.</param>
		public MessageEventArgs(XmppClient Client, XmlElement Message)
			: this(Client, null, Message)
		{
		}

		/// <summary>
		/// Event arguments for message events.
		/// </summary>
		/// <param name="Component">Component</param>
		/// <param name="Message">Message element.</param>
		public MessageEventArgs(XmppComponent Component, XmlElement Message)
			: this(null, Component, Message)
		{
		}

		private MessageEventArgs(XmppClient Client, XmppComponent Component, XmlElement Message)
		{
			XmlElement E;

			this.message = Message;
			this.client = Client;
			this.component = Component;
			this.from = XML.Attribute(Message, "from");
			this.to = XML.Attribute(Message, "to");
			this.id = XML.Attribute(Message, "id");
			this.ok = true;
			this.errorCode = 0;

			this.fromBareJid = XmppClient.GetBareJID(this.from);

			switch (XML.Attribute(Message, "type").ToLower())
			{
				case "chat":
					this.type = MessageType.Chat;
					break;

				case "error":
					this.type = MessageType.Error;
					this.ok = false;
					break;

				case "groupchat":
					this.type = MessageType.GroupChat;
					break;

				case "headline":
					this.type = MessageType.Headline;
					break;

				case "normal":
				default:
					this.type = MessageType.Normal;
					break;
			}

			SortedDictionary<string, string> Bodies = new SortedDictionary<string, string>();
			SortedDictionary<string, string> Subjects = new SortedDictionary<string, string>();

			foreach (XmlNode N in Message.ChildNodes)
			{
				E = N as XmlElement;
				if (E is null)
					continue;

				if (E.NamespaceURI == Message.NamespaceURI)
				{
					switch (E.LocalName)
					{
						case "body":
							if (string.IsNullOrEmpty(this.body))
								this.body = N.InnerText;

							string Language = XML.Attribute(E, "xml:lang");
							Bodies[Language] = N.InnerText;
							break;

						case "subject":
							if (string.IsNullOrEmpty(this.subject))
								this.subject = N.InnerText;

							Language = XML.Attribute(E, "xml:lang");
							Subjects[Language] = N.InnerText;
							break;

						case "thread":
							this.threadId = N.InnerText;
							this.parentThreadId = XML.Attribute(E, "parent");
							break;

						case "error":
							this.errorElement = E;
							this.errorCode = XML.Attribute(E, "code", 0);
							this.ok = false;

							switch (XML.Attribute(E, "type"))
							{
								case "auth":
									this.errorType = ErrorType.Auth;
									break;

								case "cancel":
									this.errorType = ErrorType.Cancel;
									break;

								case "continue":
									this.errorType = ErrorType.Continue;
									break;

								case "modify":
									this.errorType = ErrorType.Modify;
									break;

								case "wait":
									this.errorType = ErrorType.Wait;
									break;

								default:
									this.errorType = ErrorType.Undefined;
									break;
							}

							this.stanzaError = XmppClient.GetStanzaExceptionObject(E);
							this.errorText = this.stanzaError.Message;
							break;
					}
				}
				else if (this.content is null)
					this.content = E;
			}

			this.bodies = new KeyValuePair<string, string>[Bodies.Count];
			Bodies.CopyTo(this.bodies, 0);

			this.subjects = new KeyValuePair<string, string>[Subjects.Count];
			Subjects.CopyTo(this.subjects, 0);
		}

		/// <summary>
		/// The message stanza.
		/// </summary>
		public XmlElement Message { get { return this.message; } }

		/// <summary>
		/// Content of the message. For messages that are processed by registered message handlers, this value points to the element inside
		/// the message stanza, that the handler is registered to handle. For other types of messages, it represents the first custom element
		/// in the message. If no such elements are found, this value is null.
		/// </summary>
		public XmlElement Content
		{
			get { return this.content; }
			internal set { this.content = value; }
		}

		/// <summary>
		/// Type of message received.
		/// </summary>
		public MessageType Type { get { return this.type; } }

		/// <summary>
		/// From where the message was received.
		/// </summary>
		public string From
		{
			get { return this.from; }
			set
			{
				this.from = value;
				this.fromBareJid = XmppClient.GetBareJID(value);
			}
		}

		/// <summary>
		/// Bare JID of resource sending the message.
		/// </summary>
		public string FromBareJID
		{
			get { return this.fromBareJid; }
		}

		/// <summary>
		/// To whom the message was sent.
		/// </summary>
		public string To
		{
			get { return this.to; }
			set { this.to = value; }
		}

		/// <summary>
		/// ID attribute of message stanza.
		/// </summary>
		public string Id
		{
			get { return this.id; }
			set { this.id = value; }
		}

		/// <summary>
		/// Human readable subject.
		/// </summary>
		public string Subject { get { return this.subject; } }

		/// <summary>
		/// Human readable body.
		/// </summary>
		public string Body { get { return this.body; } }

		/// <summary>
		/// Thread ID.
		/// </summary>
		public string ThreadID { get { return this.threadId; } }

		/// <summary>
		/// Parent Thraed ID.
		/// </summary>
		public string ParentThreadID { get { return this.parentThreadId; } }

		/// <summary>
		/// If the response is an OK result response (true), or an error response (false).
		/// </summary>
		public bool Ok { get { return this.ok; } }

		/// <summary>
		/// Error Code
		/// </summary>
		public int ErrorCode { get { return this.errorCode; } }

		/// <summary>
		/// Error Type
		/// </summary>
		public ErrorType ErrorType { get { return this.errorType; } }

		/// <summary>
		/// Error element.
		/// </summary>
		public XmlElement ErrorElement { get { return this.errorElement; } }

		/// <summary>
		/// Any error specific text.
		/// </summary>
		public string ErrorText { get { return this.errorText; } }

		/// <summary>
		/// Any stanza error returned.
		/// </summary>
		public XmppException StanzaError { get { return this.stanzaError; } }

		/// <summary>
		/// Available set of (language,body) pairs.
		/// </summary>
		public KeyValuePair<string, string>[] Bodies { get { return this.bodies; } }

		/// <summary>
		/// Available set of (language,subject) pairs.
		/// </summary>
		public KeyValuePair<string, string>[] Subjects { get { return this.subjects; } }

		/// <summary>
		/// If end-to-end encryption was used in the request.
		/// </summary>
		public bool UsesE2eEncryption
		{
			get { return this.e2eEncryption != null; }
		}

		/// <summary>
		/// End-to-end encryption interface, if used in the request.
		/// </summary>
		public IEndToEndEncryption E2eEncryption
		{
			get { return this.e2eEncryption; }
			set { this.e2eEncryption = value; }
		}

	}
}
