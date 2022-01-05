#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import typing

from file import FluentFile
from project import Project


######################################### Class defifitions ############################################################

class FluentFormatter:
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile]):
        for file in fluent_files:
            file_data = file.read_data()
            parsed_file_data = file.parse_data(file_data)
            serialized_file_data = file.serialize_data(parsed_file_data)
            file.save_data(serialized_file_data)


######################################## Var definitions ###############################################################
project = Project()
fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)

########################################################################################################################

FluentFormatter.format(fluent_files)
