import threading
import main
from time import sleep

from logzero import logger
from twisted.internet import reactor, task

from neo.Network.NodeLeader import NodeLeader
from neo.Core.Blockchain import Blockchain
from neo.Implementations.Blockchains.LevelDB.LevelDBBlockchain import LevelDBBlockchain
from neo.Settings import settings
from logic import Logic



def node_custom_background_code():
    i = 0
    #Initializate program and fully syncronizate blockchain
    logger.info("Start synchronization")
    while True:
        sleep(8)
        logger.info("Block %s / %s", str(Blockchain.Default().Height), str(Blockchain.Default().HeaderHeight))
        if (i > 5):
            main.main_thread()
            break
        if ((Blockchain.Default().Height == Blockchain.Default().HeaderHeight) & (Blockchain.Default().Height!=0)):
            i = i + 1
    

    

def start_node():
    # Setup blockchain
    settings.setup("data/protocol.json")
    blockchain = LevelDBBlockchain(settings.chain_leveldb_path)
    print(settings.SEED_LIST)
    Blockchain.RegisterBlockchain(blockchain)
    dbloop = task.LoopingCall(Blockchain.Default().PersistBlocks)
    dbloop.start(.1)
    NodeLeader.Instance().Start()
    
    # Start a thread with custom code
    d = threading.Thread(target=node_custom_background_code)  
    d.setDaemon(True)
    d.start()
    logger.info("Ready")
    
    reactor.run()
    
    
    logger.info("Shutting down.")

    



    




        