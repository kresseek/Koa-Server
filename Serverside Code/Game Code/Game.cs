using System;
using System.Collections.Generic;
using PlayerIO.GameLibrary;

namespace MushroomsUnity3DExample {
	public class Player : BasePlayer {
        public byte noUrut = 255;  // 1-max player
        public List<Card> cardIds = new List<Card>();
        public List<Card> tongCardIds = new List<Card>();
        public bool ready = false;
        public bool koa = false;
        public bool menang = false; // sementara ada menang karena belum ada implementasi penentu winner
	}

	public class Card {
		public byte id = 255;
	}

	[RoomType("UnityMushrooms")]
	public class GameCode : Game<Player> {
		private List<Card> Deck = new List<Card>();
        private int maxPlayer = 2;
        private byte activeNoUrut = 0;
        private bool isPlaying = false;

        // This method is called when an instance of your the game is created
        public override void GameStarted() {
			// anything you write to the Console will show up in the 
			// output window of the development server
			Console.WriteLine("Game is started: " + RoomId);

            // spawn 10 toads at server start
            resetDeck();

            // respawn new toads each 5 seconds
            //AddTimer(respawntoads, 5000);
            // reset game every 2 minutes
            //AddTimer(resetgame, 120000);
            //AddTimer(turnTimeRunsOut, 10000);

		}

        // ketika waktu giliran telah habis
        private void turnTimeRunsOut(Player lastPlayer) {
            if (!isPlaying) {
                return;
            }
            
            activeNoUrut = (byte) (activeNoUrut + 1);
            if (activeNoUrut >= maxPlayer) {
                activeNoUrut = 0;
            }
            if (lastPlayer.cardIds.Count < 12)
            {
                Broadcast("TurnTimeRunsOut", new byte[] { activeNoUrut });
                return;
            }
            //ketika buang kartu sekaligus memberikan urutan pemain
            Card cardTaken = lastPlayer.cardIds[lastPlayer.cardIds.Count - 1];
            Broadcast("Buang", new byte[] {lastPlayer.noUrut, cardTaken.id }, new byte[] { activeNoUrut });
            lastPlayer.tongCardIds.Add(cardTaken);
            lastPlayer.cardIds.RemoveAt(lastPlayer.cardIds.Count - 1);
        }

        // ketika waktu player untuk mengambil kartu telah habis,
        // peringatkan player supaya segera ambil kartu
        private void getCardTimeRunsOut(Player currentPlayer)
        {
            if (!isPlaying)
            {
                return;
            }
            currentPlayer.Send("getCardTimeRunsOut", new byte[] { currentPlayer.noUrut });
        }


        private void resetgame() {
            isPlaying = false;
            activeNoUrut = 0;

			// scoring system
			Player winner = new Player();
			foreach(Player pl in Players) {
				if(pl.menang && pl.koa) {
					winner = pl;
				}
			}

			// broadcast who won the round
			if(winner.koa && winner.menang) {
				Broadcast("Chat", "Server", winner.ConnectUserId + " picked " + winner + " won this round.");
			} else {
				Broadcast("Chat", "Server", "No one won this round.");
			}

			// reset everyone's score
			foreach(Player pl in Players) {
				pl.cardIds.Clear();
                pl.tongCardIds.Clear();
                pl.koa = false;
                pl.menang = false;
                pl.noUrut = 255;
			}
            resetDeck();
			Broadcast("ToadCount", 0);
		}

		private void resetDeck() {
			//System.Random random = new System.Random();
            // create new toads if there are less than 10
            Deck.Clear();
            System.Random random = new System.Random();
            List<Card> cards = new List<Card>();

            for (int x = 0; x < 180; x++)
            {
                Card temp = new Card();
                temp.id = (byte)x;
                cards.Add(temp);
            }
            int indexRand;
            while (cards.Count > 0)
            {
                indexRand = random.Next(0, cards.Count);
                Deck.Add(cards[indexRand]);
                cards.RemoveAt(indexRand);
            }
        }

		// This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		// This method is called whenever a player joins the game
		public override void UserJoined(Player player) {
            //beri noUrut ke player
            setNoUrut(player);
            player.Send("NoUrut", new byte[] { player.noUrut });
            foreach (Player pl in Players) {
				if(pl.ConnectUserId != player.ConnectUserId) {
                   	pl.Send("PlayerJoined", player.ConnectUserId, new byte[] { player.noUrut }); // memberi tahu ke player lain kalo ada player baru
					player.Send("PlayerJoined", pl.ConnectUserId, new byte[] { pl.noUrut }, pl.ready); // memberi tahu player baru bahwa ada player lainnya
				}
			}

            // send current toadstool info to the player
            //foreach (Toad t in Toads)
            //{
            //    player.Send("Toad", t.id, t.posx, t.posz);
            //}

            if (PlayerCount >= maxPlayer) {
                Visible = false;
            }
		}

        private void setNoUrut(Player player) {
            if (PlayerCount > maxPlayer) {
                Console.WriteLine("WARNING New Player joined without no urut!. Too many players!");
                return;
            }
            for (int x = 0; x < maxPlayer; x++) {
                bool available = true;
                foreach (Player p in Players) {
                    if (p.noUrut == (byte)x) {
                        available = false;
                        break;
                    }
                }
                if (available) {
                    player.noUrut = (byte)x;
                    Console.WriteLine("info: success on getting no urut!");
                    return;
                }
            }
        }

        public override bool AllowUserJoin(Player player)
        {
            if (PlayerCount >= maxPlayer) {
                return false;
            }
            return true;
        }

        // This method is called when a player leaves the game
        public override void UserLeft(Player player) {
			Broadcast("PlayerLeft", player.ConnectUserId);
		}

		// This method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message) {
			switch(message.Type) {
                case "ReadyToPlay":
                    player.ready = true;
                    Broadcast("ReadyToPlay", new byte[] { player.noUrut });
                    bool readyAll = true;
                    foreach (Player pl in Players) {
                        if (pl.ready != true) {
                            readyAll = false;
                            break;
                        }
                    }
                    if (readyAll)
                    {
                        Broadcast("ArenaStart", new byte[] { 0 }); // no urut first player
                        foreach (Player p in Players) {
                            p.cardIds = Deck.GetRange(0, 11);
                            Deck.RemoveRange(0, 11);
                            byte[] cardIdsByte = new byte[p.cardIds.Count];
                            for (int i = 0; i < p.cardIds.Count; i++) {
                                cardIdsByte[i] = p.cardIds[i].id;
                            }
                            p.Send("GetElevenCards", cardIdsByte);
                            if (p.noUrut == 0) //first player ditentukan dari no urut (tidak acak)
                            {
                                p.Send("GetTurn", p.ConnectUserId);
                                foreach (Player pOther in Players) {
                                    if (pOther.noUrut != p.noUrut) {
                                        pOther.Send("OtherPlayerTurn", p.ConnectUserId, new byte[] { p.noUrut }); // beri tahu player lain ttg player yg sedang take turn 
                                    }
                                }
                            }
                        }
                        isPlaying = true;
                    }
                    break;
				case "AmbilKartu":
                    //called when the player take card from deck or bin
                    // server merandom kartu yang terambil oleh player

                    if (player.noUrut != activeNoUrut) {
                        break;
                    }

                    System.Random random = new System.Random();
                    bool isDeck = message.GetBoolean(0);
                    Card result = null;
                    if (isDeck)
                    {
                        // Find a card by its id
                        result = Deck[0];
                        Deck.RemoveAt(0);
                        foreach (Player pl in Players)
                        {
                            if (pl.ConnectUserId != player.ConnectUserId)
                            {
                                pl.Send("OtherGetDeck", new byte[] { player.noUrut }); // memberi tahu ke player lain kalo ada player baru
                            }
                        }
                        player.Send("DeckPicked", new byte[] { result.id }); // memberi tahu player baru bahwa ada player lainnya
                    }
                    else
                    {
                        //get card from tong on previous player (no urut - 1)
                        int prevIndex;
                        if (player.noUrut == 0)
                        {
                            prevIndex = maxPlayer - 1;
                        }else
                        {
                            prevIndex = player.noUrut - 1;
                        }
                        foreach (Player p in Players) {
                            if (p.noUrut == (byte)prevIndex) {
                                result = p.tongCardIds[p.tongCardIds.Count - 1];
                                p.tongCardIds.Remove(result);
                            }
                        }
                        Broadcast("TongPicked", new byte[] { player.noUrut , result.id });
                    }
                    player.cardIds.Add(result);
					break;
                case "Buang":
                    if (player.noUrut != activeNoUrut) {
                        break;
                    }
                    byte cardId = message.GetByteArray(0)[0];
                    result = player.cardIds.Find(delegate (Card td) { return td.id == cardId; });
                    if (result != null)
                    {
                        player.cardIds.Remove(result);
                        player.tongCardIds.Add(result);
                        byte nextNoUrut = (byte) (player.noUrut + 1); 
                        if (nextNoUrut >= maxPlayer) {
                            nextNoUrut = (byte) 0;
                        }
                        //ketika buang kartu sekaligus memberikan urutan pemain
                        Broadcast("Buang", new byte[] { player.noUrut, result.id }, new byte[] { nextNoUrut });
                        activeNoUrut = nextNoUrut;
                        foreach (Player p in Players) {
                            if (p.noUrut == nextNoUrut) {
                                ScheduleCallback(delegate () { getCardTimeRunsOut(p); }, 5000);
                                ScheduleCallback(delegate () { turnTimeRunsOut(p); }, 10000);
                                break;
                            }
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine("ERROR: (Buang) " + player.ConnectUserId + ", Card not match with server data!");
                        player.Send("ErrorBuang");
                    }
                    break;
				case "Chat":
					foreach(Player pl in Players) {
						if(pl.ConnectUserId != player.ConnectUserId) {
							pl.Send("Chat", player.ConnectUserId, message.GetString(0));
						}
					}
					break;
			}
		}
	}
}