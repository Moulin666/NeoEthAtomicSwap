## NOTE: To run this test you should have geth >= 1.7.3
## NOTE: String to run geth right is: geth --dev --rpc --rpccorsdomain "http://localhost:8545" --rpcapi="db,eth,net,web3,personal,web3"

import json
import web3
import sys
import hashlib

from web3 import Web3, HTTPProvider
from web3.contract import ConciseContract

from atomicswap import AtomicSwap

help_text = '''
Утилита имеет несколько команд:

    initiate <participant address> <amount> <secret> <timeout>
    Инициирует атомарный своп: деплоит соответствующий контракт и заполняет соответствующими значениями.
        <participant address> - адрес контракгента по атомарному свопу.
        <amount> - количество обмениваемой криптовалюты.
        <secret> - секрет, который при инициализации свопа хешируется в помощью sha256.
        <timeout> - таймаут в секундах для свопа. 

    redeem <contract address> <secret>
    Публикует секрет, чем инициирует перевод заблокированных средств.
        <contract address> - адрес контракта атомарного свопа.
        <secret> - секрет.

    refund <contract address>
    Возвращает средства с контракта атомарного свопа в случае истечения времени.
        <contract address> - адрес контракта атомарного свопа.
'''

config = open("../config/ethereum_config.json", "r").read()
config = json.loads(config)

w3 = Web3(HTTPProvider('http://localhost:8545'))

account = config["account"]
bin = open(config["binPath"], "r").read()
abi = open(config["abiPath"], "r").read()
abi = json.loads(abi)

def print_help():
    print(help_text)


def initiate(sender, args):
    atomic_swap = AtomicSwap(w3, abi, bin, sender, args)

    print("Address of contract:", atomic_swap.address)


def redeem(sender, contract_address, secret):
    atomic_swap = AtomicSwap(w3, abi, address=contract_address)
    atomic_swap.redeem(sender, secret)
    
    print("Redeemed")


def refund(sender, contract_address):
    atomic_swap = AtomicSwap(w3, abi, address=contract_address)
    atomic_swap.refund(sender)

    print("Refunded")


def show_accounts():
    print(w3.eth.accounts)


def create_accounts(N, eth_amount):
    for i in range(1, N):
        w3.personal.newAccount('password')
        w3.personal.unlockAccount(w3.eth.accounts[i], 'password')
        w3.eth.sendTransaction({'to': w3.eth.accounts[i], 'from': w3.eth.accounts[0], 'value': eth_amount})



if len(sys.argv) == 1:
    print_help()
elif sys.argv[1] == "initiate":
    initiate(account, [sys.argv[2], int(sys.argv[3]), w3.toBytes(hexstr=hashlib.sha256(str(sys.argv[4]).encode()).hexdigest()), int(sys.argv[5])])
elif sys.argv[1] == "redeem":
    redeem(account, sys.argv[2], sys.argv[3])
elif sys.argv[1] == "refund":
    refund(account, sys.argv[2])
elif sys.argv[1] == "accounts":
    show_accounts()
elif sys.argv[1] == "create":
    create_accounts(int(sys.argv[2]), int(sys.argv[3]))
else:
    print_help()
